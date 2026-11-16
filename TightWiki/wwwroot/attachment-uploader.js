/*
 * Uploads page attachments in pieces, so files of any size can be uploaded and an interrupted
 * upload continues where it stopped instead of starting over.
 *
 *   const uploader = new TwAttachmentUploader({
 *       startUrl: "/File/Upload/Start/my-page",   //Starts an upload for the page.
 *       uploadUrl: "/File/Upload/",               //Base of the per-upload URLs.
 *       statusElement: document.getElementById("uploadStatus"),
 *       text: { ... },                            //Translated text, see defaultText below.
 *       onFileDone: (result) => { },              //After each file is attached.
 *       onAllDone: () => { }                      //After the queue is empty.
 *   });
 *   uploader.upload(fileList);                    //Or upload([{ file, name }]) to choose the names.
 */
(function () {
    "use strict";

    const defaultText = {
        waiting: "Waiting",
        uploading: "Uploading",
        finishing: "Finishing",
        done: "Attached",
        unchanged: "Unchanged, already attached",
        cancelled: "Cancelled",
        failed: "Failed",
        retrying: "Connection lost, retrying",
        cancel: "Cancel",
        leaveWarning: "Files are still uploading. Leaving now will cancel them."
    };

    const maxRetries = 8;

    function formatSize(bytes) {
        const units = ["B", "KB", "MB", "GB", "TB"];
        let value = bytes, unit = 0;
        while (value >= 1024 && unit < units.length - 1) { value /= 1024; unit++; }
        return (unit === 0 ? value : value.toFixed(1)) + " " + units[unit];
    }

    class UploadError extends Error {
        constructor(message, status, body) {
            super(message);
            this.status = status;
            this.body = body || {};
        }
        //Worth trying again: the connection dropped or the server had a passing problem.
        get isTransient() {
            return this.status === 0 || this.status === 499 || this.status === 502 || this.status === 503 || this.status === 504;
        }
    }

    //Sends a request and returns the parsed JSON response, rejecting with an UploadError for anything but success.
    function send(method, url, body, onProgress, onXhr) {
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();
            xhr.open(method, url);
            xhr.setRequestHeader("Accept", "application/json");
            if (body !== undefined && !(body instanceof Blob)) {
                xhr.setRequestHeader("Content-Type", "application/json");
                body = JSON.stringify(body);
            } else if (body instanceof Blob) {
                xhr.setRequestHeader("Content-Type", "application/octet-stream");
            }
            if (onProgress) {
                xhr.upload.onprogress = (e) => { if (e.lengthComputable) onProgress(e.loaded); };
            }
            xhr.onload = () => {
                let json = null;
                try { json = JSON.parse(xhr.responseText); } catch { /* Not JSON, such as a login page. */ }
                if (xhr.status >= 200 && xhr.status < 300 && json) {
                    resolve(json);
                } else {
                    const message = (json && json.message) || (xhr.status + " " + xhr.statusText);
                    reject(new UploadError(message, json ? xhr.status : (xhr.status || 0), json));
                }
            };
            xhr.onerror = () => reject(new UploadError("Network error", 0));
            xhr.onabort = () => reject(new UploadError("Cancelled", -1));
            if (onXhr) onXhr(xhr);
            xhr.send(body);
        });
    }

    class TwAttachmentUploader {
        constructor(options) {
            this.options = options;
            this.text = Object.assign({}, defaultText, options.text || {});
            this.queue = [];
            this.running = false;
            this.active = 0;

            window.addEventListener("beforeunload", (e) => {
                if (this.active > 0) {
                    e.preventDefault();
                    e.returnValue = this.text.leaveWarning;
                    return this.text.leaveWarning;
                }
            });
        }

        //Accepts a FileList, an array of File, or an array of { file, name }. Resolves with one result per file.
        upload(files) {
            const items = Array.from(files).map(f => (f instanceof Blob) ? { file: f, name: f.name } : f);
            const promises = items.map(item => new Promise(resolve => {
                item.row = this.createRow(item);
                item.resolve = resolve;
                this.queue.push(item);
                this.active++;
            }));
            this.run();
            return Promise.all(promises);
        }

        async run() {
            if (this.running) return;
            this.running = true;
            while (this.queue.length > 0) {
                const item = this.queue.shift();
                let result;
                try {
                    result = await this.uploadOne(item);
                } catch (err) {
                    result = { success: false, fileName: item.name, message: err.message };
                }
                this.active--;
                item.resolve(result);
                if (result.success && this.options.onFileDone) this.options.onFileDone(result);
            }
            this.running = false;
            if (this.options.onAllDone) this.options.onAllDone();
        }

        async uploadOne(item) {
            const { file, name, row } = item;
            const size = file.size;

            if (item.cancelled) {
                row.end(this.text.cancelled, "secondary");
                return { success: false, fileName: name, cancelled: true };
            }

            let uploadId = null;
            try {
                row.setState(this.text.uploading, "primary");
                const started = await send("POST", this.options.startUrl, { fileName: name, size: size });
                uploadId = started.uploadId;
                item.uploadId = uploadId;
                const pieceSize = started.maxPieceSize;

                let offset = started.received || 0;
                let retries = 0;

                while (offset < size) {
                    if (item.cancelled) throw new UploadError("Cancelled", -1);
                    const end = Math.min(offset + pieceSize, size);
                    const pieceStart = offset;
                    try {
                        const response = await send("PUT", this.options.uploadUrl + uploadId + "/" + pieceStart, file.slice(pieceStart, end),
                            (loaded) => row.setProgress(pieceStart + loaded, size),
                            (xhr) => { item.xhr = xhr; });
                        offset = response.received;
                        retries = 0;
                        row.setState(this.text.uploading, "primary");
                    } catch (err) {
                        if (err.status === -1) throw err;
                        if (err.status === 409 && typeof err.body.received === "number") {
                            offset = err.body.received; //The server has a different amount than we thought, continue from there.
                            continue;
                        }
                        if (!err.isTransient || ++retries > maxRetries) throw err;

                        row.setState(this.text.retrying, "warning");
                        await new Promise(resolve => {
                            //Cancelling during the wait ends it at once.
                            const timer = setTimeout(resolve, Math.min(30000, 1000 * Math.pow(2, retries - 1)));
                            item.wake = () => { clearTimeout(timer); resolve(); };
                        });
                        item.wake = null;
                        if (item.cancelled) throw new UploadError("Cancelled", -1);
                        try {
                            offset = (await send("GET", this.options.uploadUrl + uploadId)).received;
                        } catch (statusErr) {
                            if (!statusErr.isTransient) throw statusErr;
                        }
                    }
                    row.setProgress(offset, size);
                }

                row.setState(this.text.finishing, "primary");
                row.setProgress(size, size);
                const result = await send("POST", this.options.uploadUrl + uploadId + "/Complete");
                row.done(result.changed === false ? this.text.unchanged : this.text.done);
                return result;
            } catch (err) {
                if (uploadId) {
                    send("DELETE", this.options.uploadUrl + uploadId).catch(() => { });
                }
                if (err.status === -1) {
                    row.end(this.text.cancelled, "secondary");
                    return { success: false, fileName: name, cancelled: true };
                }
                row.fail(this.text.failed + ": " + err.message);
                return { success: false, fileName: name, message: err.message };
            }
        }

        createRow(item) {
            const text = this.text;
            const container = document.createElement("div");
            container.className = "border rounded p-2 mb-2";

            const header = document.createElement("div");
            header.className = "d-flex justify-content-between align-items-center gap-2 flex-wrap";

            const title = document.createElement("span");
            title.className = "fw-semibold text-break";
            title.textContent = item.name;

            const right = document.createElement("span");
            right.className = "d-flex align-items-center gap-2 small text-muted";

            const state = document.createElement("span");
            state.textContent = text.waiting;

            const amount = document.createElement("span");
            amount.style.fontVariantNumeric = "tabular-nums";
            amount.textContent = "0 B / " + formatSize(item.file.size);

            const cancel = document.createElement("button");
            cancel.type = "button";
            cancel.className = "btn btn-outline-secondary btn-sm py-0";
            cancel.textContent = text.cancel;
            cancel.addEventListener("click", () => {
                item.cancelled = true;
                if (item.xhr) item.xhr.abort();
                if (item.wake) item.wake();
                cancel.disabled = true;
            });

            right.append(state, amount, cancel);
            header.append(title, right);

            const progress = document.createElement("div");
            progress.className = "progress mt-1";
            progress.style.height = "6px";
            progress.setAttribute("role", "progressbar");
            progress.setAttribute("aria-label", item.name);
            progress.setAttribute("aria-valuemin", "0");
            progress.setAttribute("aria-valuemax", "100");

            const bar = document.createElement("div");
            bar.className = "progress-bar";
            bar.style.width = "0%";
            progress.append(bar);

            container.append(header, progress);
            this.options.statusElement.append(container);

            return {
                setProgress(sent, total) {
                    const percent = total > 0 ? Math.floor(sent * 100 / total) : 100;
                    bar.style.width = percent + "%";
                    progress.setAttribute("aria-valuenow", String(percent));
                    amount.textContent = formatSize(sent) + " / " + formatSize(total);
                },
                setState(label, color) {
                    state.textContent = label;
                    bar.className = "progress-bar" + (color ? " bg-" + color : "");
                },
                end(label, color) {
                    state.textContent = label;
                    bar.className = "progress-bar" + (color ? " bg-" + color : "");
                    cancel.remove();
                },
                done(label) {
                    state.textContent = label;
                    bar.className = "progress-bar bg-success";
                    cancel.remove();
                },
                fail(message) {
                    state.textContent = message;
                    state.className = "text-danger";
                    bar.className = "progress-bar bg-danger";
                    cancel.remove();
                }
            };
        }
    }

    window.TwAttachmentUploader = TwAttachmentUploader;
})();

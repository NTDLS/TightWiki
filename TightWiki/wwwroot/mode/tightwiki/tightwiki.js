// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: https://codemirror.net/5/LICENSE

/*
 * =====================================================================================
 *
 *       Filename:  mode/tightwiki/tightwiki.js
 *
 *    Description:  CodeMirror mode for tightwiki
 *
 *        Created:  10/30/2022
 *
 *         Author:  Josh Patterson
 *        Company:  NetworkDLS
 *
 * =====================================================================================
 */

(function (mod) {
    if (typeof exports == "object" && typeof module == "object") // CommonJS
        mod(require("../../lib/codemirror"));
    else if (typeof define == "function" && define.amd) // AMD
        define(["../../lib/codemirror"], mod);
    else // Plain browser env
        mod(CodeMirror);
})(function (CodeMirror) {
    "use strict";

    CodeMirror.defineMode("tightwiki", function () {
        var functions = ["##set", "##get", "##color", "##tag", "##searchlist", "##taglist", "##searchcloud", "##tagglossary", "##recentlymodified", "##textglossary", "##tagcloud", "##image", "##file", "##related", "##tags", "##editlink", "##inject", "##include", "##br", "##hr", "##history", "##attachments", "##toc", "##title", "##navigation", "##name", "##created", "##lastmodified", "##appversion"],
            instructions = ["@@protect", "@@draft", "@@review", "@@deprecate", "@@include", "@@template"],
            scopes = ["bullets", "alert", "background", "collapse", "callout", "code", "foreground", "jumbotron", "card"];

        function basicToken(stream, state) {

            var cur = '';
            var ch = stream.next();
            // comment
            if (state.blockComment) {
                if (ch == "-" && stream.match("-;", true)) {
                    state.blockComment = false;
                } else if (stream.skipTo("--;")) {
                    stream.next();
                    stream.next();
                    stream.next();
                    state.blockComment = false;
                } else {
                    stream.skipToEnd();
                }
                return "comment";
            }

            if (ch == ";") {
                if (stream.match("--", true)) {
                    if (!stream.match("-", false)) {  // Except ;--- is not a block comment
                        state.blockComment = true;
                        return "comment";
                    }
                }
                stream.skipToEnd();
                return "comment";
            }

            if (ch == "*") {
                stream.eatWhile(/\*/);
                cur = stream.current();
                if (cur == "**") {
                    stream.skipTo("**");
                    return "strong";
                }
            }

            if (ch == "/") {
                stream.eatWhile(/\//);
                cur = stream.current();
                if (cur == "//") {
                    stream.skipTo("//");
                    return "italics";
                }
                return "";
            }

            if (ch == "_") {
                stream.eatWhile(/\_/);
                cur = stream.current();
                if (cur == "__") {
                    stream.skipTo("__");
                    return "underline";
                }
                return "";
            }

            if (ch == "~") {
                stream.eatWhile(/\~/);
                cur = stream.current();
                if (cur == "~~") {
                    stream.skipTo("~~");
                    return "strike";
                }
                return "";
            }

            if (ch == "=") {
                stream.eatWhile(function (ch) {
                    if (ch == '=') {
                        return true;
                    }
                    return false;
                });
                cur = stream.current();
                if (cur.startsWith("==")) {
                    stream.skipToEnd();
                    return "comment";
                }
                return "";
            }

            // Links
            if (ch == '[') {
                stream.eatWhile(/\[/);
                cur = stream.current();
                if (cur == "[[") {
                    stream.skipTo(']');
                    stream.eatWhile(']');
                    return "wikilink";
                }
            }

            // string
            if (ch == '"') {
                stream.skipTo('"');
                return "string";
            }
            if (ch == "'") {
                stream.skipTo("'");
                return "string-2";
            }


            // functions
            if (ch == '#') {
                stream.eatWhile(function (ch) {
                    if (/\w/.test(ch) || ch == '#') {
                        return true;
                    }
                    return false;
                });

                cur = stream.current().toLowerCase();
                if (functions.indexOf(cur) !== -1) {
                    return "variable-3 strong";
                }
            }
            // instructions
            if (ch == '@') {
                stream.eatWhile(function (ch) {
                    if (/\w/.test(ch) || ch == '@') {
                        return true;
                    }
                    return false;
                });

                cur = stream.current().toLowerCase();
                if (instructions.indexOf(cur) !== -1) {
                    return "variable-2 strong";
                }
            }
            //Enter scope
            if (ch == '{') {
                stream.eatWhile(function (ch) {
                    if (ch == '{') {
                        return true;
                    }
                    return false;
                });

                cur = stream.current();

                if (cur == "{{{") {
                    state.scopeLevel++;
                    state.inScopeFirstLine = true;
                    return "";
                }
            }
            //Exit scope
            if (ch == '}') {
                stream.eatWhile(function (ch) {
                    if (ch == '}') {
                        return true;
                    }
                    return false;
                });

                cur = stream.current();

                if (cur == "}}}") {
                    state.scopeLevel--;
                    state.inScopeFirstLine = false;
                    return "";
                }
            }

            // application args
            if (ch == '$') {
                var ch1 = stream.peek();
                if (ch1 == '{') {
                    stream.skipTo('}');
                    stream.eat('}');
                    return "variable-2";
                }
            }

            // Everything else
            stream.eatWhile(/\w/);
            cur = stream.current().toLowerCase();

            if (state.inScopeFirstLine == true && scopes.indexOf(cur) !== -1) {
                state.inScopeFirstLine = false;
                return "variable-2 strong";
            }

            if (instructions.indexOf(cur) !== -1) {
                state.inScopeFirstLine = false;
                return "variable-2 strong";
            }
        }

        return {
            startState: function () {
                return {
                    blockComment: false,
                    extenStart: false,
                    extenSame: false,
                    extenInclude: false,
                    extenExten: false,
                    extenPriority: false,
                    extenApplication: false,
                    scopeLevel: 0,
                    inScopeFirstLine: false
                };
            },
            token: function (stream, state) {

                var cur = '';
                if (stream.eatSpace()) return null;
                // extension started
                if (state.extenStart) {
                    stream.eatWhile(/[^\s]/);
                    cur = stream.current();
                    if (/^=>?$/.test(cur)) {
                        state.extenExten = true;
                        state.extenStart = false;
                        return "strong";
                    } else {
                        state.extenStart = false;
                        stream.skipToEnd();
                        return "error";
                    }
                } else if (state.extenExten) {
                    // set exten and priority
                    state.extenExten = false;
                    state.extenPriority = true;
                    stream.eatWhile(/[^,]/);
                    if (state.extenInclude) {
                        stream.skipToEnd();
                        state.extenPriority = false;
                        state.extenInclude = false;
                    }
                    if (state.extenSame) {
                        state.extenPriority = false;
                        state.extenSame = false;
                        state.extenApplication = true;
                    }
                    return "tag";
                } else if (state.extenPriority) {
                    state.extenPriority = false;
                    state.extenApplication = true;
                    stream.next(); // get comma
                    if (state.extenSame) return null;
                    stream.eatWhile(/[^,]/);
                    return "number";
                } else if (state.extenApplication) {
                    stream.eatWhile(/,/);
                    cur = stream.current();
                    if (cur === ',') return null;
                    stream.eatWhile(/\w/);
                    cur = stream.current().toLowerCase();
                    state.extenApplication = false;
                    if (instructions.indexOf(cur) !== -1) {
                        return "strong";
                    }
                } else {
                    return basicToken(stream, state);
                }

                return null;
            },

            blockCommentStart: ";--",
            blockCommentEnd: "--;",
            lineComment: "=="
        };
    });

    CodeMirror.defineMIME("text/x-tightwiki", "tightwiki");

});

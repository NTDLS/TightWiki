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
      var functions = ["##systememojicategorylist", "##systememojilist", "##set", "##seq", "##get", "##color", "##searchlist", "##taglist", "##searchcloud", "##tagglossary", "##recentlymodified", "##textglossary", "##tagcloud", "##image", "##file", "##related", "##tags", "##editlink", "##inject", "##include", "##br", "##hr", "##revisions", "##attachments", "##toc", "##title", "##navigation", "##name", "##created", "##lastmodified", "##sitename", "##appversion", "##profileglossary", "##profilelist", "##namespaceglossary", "##namespacelist", "##namespace", "##snippet", "##description"],
            instructions = ["@@protect", "@@hidefooterlastmodified", "@@tags", "@@title", "@@hidefootercomments", "@@nocache", "@@draft", "@@review", "@@deprecate", "@@include", "@@template"],
            scopes = ["bullets", "alert", "background", "collapse", "callout", "code", "foreground", "jumbotron", "card", "table", "stripedtable", "definesnippet", "order", "blockquote", "figure"];

        function basicToken(stream, state) {

            var cur = '';
            var ch = stream.next();
            var cc = '';

            // Comment (Single-line)
            if (ch == ";") {
                if (stream.peek() == ';') {
                    stream.skipToEnd();
                    return "comment";
                }
            }
            // Highlight
            if (ch == "!") {
                if (stream.peek() == '!') {
                    if (stream.skipTo("!!")) {
                        stream.eatWhile('!');
                        return "highlight";
                    }
                }
            }
            // Emoji
            if (ch == "%") {
                if (stream.peek() == '%') {
                    if (stream.skipTo("%%")) {
                        stream.eatWhile('%');
                        return "strong";
                    }
                }
            }
            // Bold
            if (ch == "*") {
                if (stream.peek() == '*') {
                    if (stream.skipTo("**")) {
                        stream.eatWhile('*');
                        return "strong";
                    }
                }
            }
            // Italics
            if (ch == "/") {
                if (stream.peek() == '/') {
                    if (stream.skipTo("//")) {
                        stream.eatWhile('/');
                        return "italics";
                    }
                }
            }
            // Underline
            if (ch == "_") {
                if (stream.peek() == '_') {
                    if (stream.skipTo("__")) {
                        stream.eatWhile('_');
                        return "underline";
                    }
                }
            }
            // Strike though
            if (ch == "~") {
                if (stream.peek() == '~') {
                    if (stream.skipTo("~~")) {
                        stream.eatWhile('~');
                        return "strike";
                    }
                }
            }
            // Headings
            if (ch == "=") {
                if (stream.peek() == '=') {
                    stream.skipToEnd();
                    return "heading";
                }
            }

            // Links
            if (ch == '[') {
                stream.eatWhile(/\[/);
                cur = stream.current();
                if (cur == "[[") {
                    stream.skipTo(']');
                    stream.eatWhile(']');
                    return "atom";
                }
            }

            // String "..."
            if (ch == '"') {
                stream.skipTo('"');
                return "string";
            }
            
            // String '...'
            if (ch == "'") {
                stream.skipTo("'");
                return "string-2";
            }

            // Instructions
            cc = '#';
            if (ch == cc) {
                if (stream.peek() == cc) {
                    stream.eat(cc);
                    stream.eatWhile(/\w/);
                    cur = stream.current().toLowerCase();
                    if (functions.indexOf(cur) !== -1) {
                        return "variable-3 strong";
                    }
                }
            }

            // Instructions
            cc = '@';
            if (ch == cc) {
                if (stream.peek() == cc) {
                    stream.eat(cc);
                    stream.eatWhile(/\w/);
                    cur = stream.current().toLowerCase();
                    if (instructions.indexOf(cur) !== -1) {
                        return "variable-2 strong";
                    }
                }
            }

            //Enter scope
            if (ch == '{') {
                stream.eatWhile('{');
                if (stream.current() == "{{") {
                    state.scopeLevel++;
                    state.inScopeFirstLine = true;
                    return "";
                }
            }

            //Exit scope
            if (ch == '}') {
                stream.eatWhile('}');
                if (stream.current() == "}}") {
                    state.scopeLevel--;
                    state.inScopeFirstLine = false;
                    return "";
                }
            }

            // Variables
            if (ch == '$') {
                if (stream.peek() == '{') {
                    stream.skipTo('}');
                    stream.eat('}');
                    return "variable-2";
                }
            }

            // Everything else
            stream.eatWhile(/\w/);
            cur = stream.current().toLowerCase();

            //Scope functions (must be a scope function and be on the first line of the scrop definition)
            if (state.inScopeFirstLine == true && scopes.indexOf(cur) !== -1) {
                state.inScopeFirstLine = false;
                return "variable-2 strong";
            }
        }

        return {
            startState: function () {
                return {
                    scopeLevel: 0,
                    inScopeFirstLine: false
                };
            },
            token: function (stream, state) {
                if (stream.eatSpace()) return null;
                return basicToken(stream, state);
            }
        };
    });

    CodeMirror.defineMIME("text/x-tightwiki", "tightwiki");
});

// use strict

window.applications = window.applications ?? {};
window.applications.schema = {
    initializeEditor: () => {
        const container = document.getElementById("editor-container");
        const editor = monaco.editor.create(container, {
            value: ['function x() {', '\tconsole.log("Hello world!");', '}'].join('\n'),
            language: 'javascript'
        });
    }
};

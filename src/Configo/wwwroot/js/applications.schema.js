// use strict

import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";

const state = {};

class ApplicationsSchema {
    #dotNetRef;
    #schema;
    #editor;
    constructor() {
    }
    
    initialize(dotNetRef, schema) {
        this.#dotNetRef = dotNetRef;
        this.#schema = schema;

        monaco.editor.setTheme("vs-dark");
        const container = document.getElementById("editor-container");
        this.#editor = monaco.editor.create(container, {
            value: schema,
            language: 'json',
            automaticLayout: true
        });

        this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
            await this.save();
        });
    }
    
    async save() {
        const schema = this.#editor.getValue();
        await this.#dotNetRef.invokeMethodAsync("Save", schema);
    }
}

window.MonacoEnvironment = {
    getWorkerUrl: function (moduleId, label) {
        if (label === 'json') {
            return './dist/vs/language/json/json.worker.js';
        }
        if (label === 'css' || label === 'scss' || label === 'less') {
            return './dist/vs/language/css/css.worker.js';
        }
        if (label === 'html' || label === 'handlebars' || label === 'razor') {
            return './dist/vs/language/html/html.worker.js';
        }
        if (label === 'typescript' || label === 'javascript') {
            return './dist/vs/language/typescript/ts.worker.js';
        }
        return './dist/vs/editor/editor.worker.js';
    }
};
window.applications = window.applications ?? {};
window.applications.schema = new ApplicationsSchema();



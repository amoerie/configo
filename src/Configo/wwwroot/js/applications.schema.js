// use strict

import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";

class ApplicationsSchema {
    #dotNetRef;
    #model;
    #editor;
    constructor() {
    }
    
    initialize(dotNetRef, schema) {
        this.#dotNetRef = dotNetRef;

        monaco.editor.setTheme("vs-dark");
        
        // Allow the editor to make HTTP requests to fetch referenced JSON schemas
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({ 
            validate: true, 
            allowComments: false, 
            schemas: [{
                fileMatch: [ "schema.json" ],
                uri: "https://json-schema.org/draft-04/schema",
            }], 
            enableSchemaRequest: true
        });

        this.#model = monaco.editor.createModel(schema, "json", monaco.Uri.parse("internal://server/schema.json"));
        const container = document.getElementById("editor-container");
        this.#editor = monaco.editor.create(container, {
            model: this.#model,
            automaticLayout: true
        });

        this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
            await this.save();
        });
    }
    
    destroy() {
        this.#model.dispose();
        this.#editor.dispose();
        this.#model = null;
        this.#editor = null;
        this.#dotNetRef = null;
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



﻿// use strict

import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";

class Schema {
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
                // If we give the model a name that matches wich this filematch
                // monaco will use this schema file for validation
                fileMatch: [ "schema.json" ],
                uri: "https://json-schema.org/draft-04/schema",
            }], 
            enableSchemaRequest: true
        });

        this.#model = monaco.editor.createModel(schema, "json", monaco.Uri.parse("internal://server/schema.json"));
        const container = document.getElementById("schema-editor-container");
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

window.schema = new Schema();



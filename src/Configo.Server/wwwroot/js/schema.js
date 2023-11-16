// use strict

import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";

export class Schema {
    /**
     * @type {Object}
     */
    #dotNetRef;

    /**
     * @type {int}
     */
    #applicationId;

    /**
     * @type {monaco.editor.ITextModel}
     */
    #model;

    /**
     * @type {monaco.editor.IStandaloneCodeEditor}
     */
    #editor;
    constructor() {
    }
    initialize(dotNetRef, applicationId, schema) {
        this.#dotNetRef = dotNetRef;
        this.#applicationId = applicationId;

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

        if(!container)
        {
            console.warn("Container for monaco editor not present");
            return;
        }
        
        this.#editor = monaco.editor.create(container, {
            model: this.#model,
            automaticLayout: true
        });

        this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
            await this.save();
        });
        
        this.#editor.layout({
            width: container.clientWidth,
            height: document.documentElement.clientHeight - container.offsetTop
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
        const applicationId = this.#applicationId;

        const data = { 
            schema: schema
        };
        await fetch(`/api/applications/${applicationId}/schema`, {
            method: "POST",
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(data),
        })
        
        await this.#dotNetRef.invokeMethodAsync("OnSave");
    }
}

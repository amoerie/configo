// use strict
import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";
class Variables {
    #dotNetRef;
    #model;
    #editor;
    constructor() {
    }
    
    initialize(dotNetRef, config, schema) {
        this.#dotNetRef = dotNetRef;

        monaco.editor.setTheme("vs-dark");
        
        // Allow the editor to make HTTP requests to fetch referenced JSON schemas
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({ 
            validate: true, 
            allowComments: false, 
            schemas: [{
                // If we give the model a name that matches wich this filematch
                // monaco will use this schema file for validation
                fileMatch: [ "config.json" ],
                schema: schema,
                uri: ""
            }] 
        });

        this.#model = monaco.editor.createModel(config, "json", monaco.Uri.parse("internal://server/config.json"));
        const container = document.getElementById("variables-editor-container");
        this.#editor = monaco.editor.create(container, {
            model: this.#model,
            automaticLayout: true
        });

        this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
            await this.save();
        });
    }
    
    update(config, schema) {
        // Allow the editor to make HTTP requests to fetch referenced JSON schemas
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            allowComments: false,
            schemas: [{
                // If we give the model a name that matches wich this filematch
                // monaco will use this schema file for validation
                fileMatch: [ "config.json" ],
                schema: schema,
                uri: ""
            }]
        });

        this.#model.setValue(config)
    }
    
    destroy() {
        this.#model.dispose();
        this.#editor.dispose();
        this.#model = null;
        this.#editor = null;
        this.#dotNetRef = null;
    }
    
    async save() {
        const config = this.#editor.getValue();
        await this.#dotNetRef.invokeMethodAsync("Save", config);
    }
}

window.variables = new Variables();



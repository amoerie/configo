// use strict
import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";
export class Variables {
    #dotNetRef;
    #model;
    #modelUri;
    #editor;
    constructor() {
    }

    /**
     * @param {Object} dotNetRef
     * @param {string} config
     * @param {string} schema
     * @param {boolean} isReadonly
     */
    initialize(dotNetRef, config, schema, isReadonly) {
        this.#dotNetRef = dotNetRef;

        monaco.editor.setTheme("vs-dark");

        this.#modelUri = monaco.Uri.parse("internal://server/config.json");
        this.#model = monaco.editor.createModel(config, "json", this.#modelUri);

        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            allowComments: false,
            schemas: [{
                // If we give the model a name that matches wich this filematch
                // monaco will use this schema file for validation
                uri: "http://server/config-schema",
                fileMatch: [ this.#modelUri.toString() ],
                schema: JSON.parse(schema),
            }],
            enableSchemaRequest: false
        });
        
        const container = document.getElementById("variables-editor-container");
        
        if(!container) 
        {
            console.warn("Container for monaco editor not present");
            return;
        }
        
        this.#editor = monaco.editor.create(container, {
            model: this.#model,
            automaticLayout: true,
            readonly: isReadonly
        });

        this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
            await this.save();
        });
    }

    /**
     * @param {string} config
     * @param {string} schema
     * @param {boolean} isReadonly
     */
    update(config, schema, isReadonly) {
        // Allow the editor to make HTTP requests to fetch referenced JSON schemas
        monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
            validate: true,
            allowComments: false,
            schemas: [{
                // If we give the model a name that matches wich this filematch
                // monaco will use this schema file for validation
                uri: "http://server/config-schema",
                fileMatch: [ this.#modelUri.toString() ],
                schema: JSON.parse(schema),
            }],
            enableSchemaRequest: false
        });
        
        this.#model.dispose();
        this.#model = monaco.editor.createModel(config, "json", monaco.Uri.parse("internal://server/config.json"));
        this.#editor.setModel(this.#model);
        this.#editor.updateOptions({ readOnly: isReadonly });
    }
    
    destroy() {
        this.#model?.dispose();
        this.#editor?.dispose();
        this.#model = null;
        this.#editor = null;
        this.#dotNetRef = null;
    }
    
    async save() {
        const config = this.#editor.getValue();
        await this.#dotNetRef.invokeMethodAsync("Save", config);
    }
}

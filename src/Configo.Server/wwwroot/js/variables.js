// use strict
import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";
export class Variables {
    /**
     * @type {Object}
     */
    #dotNetRef;
    
    /**
     * @type {string | null}
     */
    #schema;
    
    /**
     * @type {string | null}
     */
    #diffSchema;

    /**
     * @type {monaco.editor.ITextModel}
     */
    #model;

    /**
     * @type {monaco.editor.ITextModel}
     */
    #originalModel;

    /**
     * @type {monaco.editor.ITextModel}
     */
    #modifiedModel;

    /**
     * @type {monaco.Uri}
     */
    #modelUri;

    /**
     * @type {monaco.Uri}
     */
    #originalModelUri;

    /**
     * @type {monaco.Uri}
     */
    #modifiedModelUri;

    /**
     * @type {monaco.editor.IStandaloneCodeEditor}
     */
    #editor;

    /**
     * @type {monaco.editor.IStandaloneDiffEditor}
     */
    #diffEditor;
    
    constructor() {
    }
    
    #disposeStandalone() {
        this.#model?.dispose();
        this.#editor?.dispose();
        this.#model = null;
        this.#editor = null;
        this.#schema = null;
    }
    
    #disposeDiff() {
        this.#originalModel?.dispose();
        this.#modifiedModel?.dispose();
        this.#diffEditor?.dispose();
        this.#originalModel = null;
        this.#modifiedModel = null;
        this.#diffEditor = null;
        this.#originalModelUri = null;
        this.#modifiedModelUri = null;
        this.#diffSchema = null;
    }

    /**
     * @param {Object} dotNetRef
     * @param {string} config
     * @param {string} schema
     * @param {boolean} isReadonly
     */
    updateEditor(dotNetRef, config, schema, isReadonly) {
        this.#dotNetRef = dotNetRef;
        this.#disposeDiff();

        if(!this.#modelUri) {
            this.#modelUri = monaco.Uri.parse("internal://server/config.json");
        }
        
        if(this.#schema !== schema) {
            this.#schema = schema;
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
        }
        
        if(!this.#editor) {
            monaco.editor.setTheme("vs-dark");

            this.#model = monaco.editor.createModel(config, "json", this.#modelUri);

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
        else {
            this.#model.dispose();
            this.#model = monaco.editor.createModel(config, "json", monaco.Uri.parse("internal://server/config.json"));
            this.#editor.setModel(this.#model);
            this.#editor.updateOptions({ readOnly: isReadonly });
        }
    }
    
    /**
     * @param {Object} dotNetRef
     * @param {string} originalConfig
     * @param {string} modifiedConfig
     * @param {string} schema
     */
    updateDiffEditor(dotNetRef, originalConfig, modifiedConfig, schema) {
        this.#dotNetRef = dotNetRef;
        this.#disposeStandalone();

        if(!this.#originalModelUri || !this.#modifiedModelUri) {
            this.#originalModelUri = monaco.Uri.parse("internal://server/original-config.json");
            this.#modifiedModelUri = monaco.Uri.parse("internal://server/modified-config.json");
        }
        
        if(this.#diffSchema !== schema) {
            this.#diffSchema = schema;
            monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
                validate: true,
                allowComments: false,
                schemas: [{
                    // If we give the model a name that matches wich this filematch
                    // monaco will use this schema file for validation
                    uri: "http://server/config-schema",
                    fileMatch: [ this.#originalModelUri.toString(), this.#modifiedModelUri.toString() ],
                    schema: JSON.parse(schema),
                }],
                enableSchemaRequest: false
            });
        }
        
        if(!this.#diffEditor) {
            monaco.editor.setTheme("vs-dark");

            this.#originalModel = monaco.editor.createModel(originalConfig, "json", this.#originalModelUri);
            this.#modifiedModel = monaco.editor.createModel(originalConfig, "json", this.#modifiedModelUri);

            const container = document.getElementById("variables-editor-container");

            if(!container)
            {
                console.warn("Container for monaco editor not present");
                return;
            }

            this.#diffEditor = monaco.editor.createDiffEditor(container, {
                originalEditable: false,
                automaticLayout: true,
            });

            this.#diffEditor.setModel({
                original: this.#originalModel,
                modified: this.#modifiedModel
            });
        }
        else {
            this.#originalModel.dispose();
            this.#modifiedModel.dispose();
            this.#originalModel = monaco.editor.createModel(originalConfig, "json", this.#originalModelUri);
            this.#modifiedModel = monaco.editor.createModel(modifiedConfig, "json", this.#modifiedModelUri);
            this.#diffEditor.setModel({
                original: this.#originalModel,
                modified: this.#modifiedModel
            });
        }
    }

    destroy() {
        this.#disposeStandalone();
        this.#disposeDiff();
        this.#dotNetRef = null;
    }
    
    async save() {
        const config = this.#editor.getValue();
        await this.#dotNetRef.invokeMethodAsync("Save", config);
    }
}

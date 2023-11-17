// use strict
import * as monaco from "monaco-editor/esm/vs/editor/editor.main.js";

export class Variables {
    /**
     * @type {Object}
     */
    #dotNetRef;

    /**
     * @type {HTMLDivElement}
     */
    #editorContainer;

    /**
     * @type {HTMLDivElement}
     */
    #diffEditorContainer;

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

    #disposeEditorModels() {
        this.#model?.dispose();
        this.#model = null;
        this.#modelUri = null;
        this.#schema = null;
        this.#editor?.setModel(null);
    }

    #disposeDiffEditorModels() {
        this.#originalModel?.dispose();
        this.#modifiedModel?.dispose();
        this.#originalModel = null;
        this.#modifiedModel = null;
        this.#originalModelUri = null;
        this.#modifiedModelUri = null;
        this.#diffSchema = null;
        this.#diffEditor?.setModel(null);
    }
    
    #initializeEditorContainers() {
        if (!this.#editorContainer) {
            const container = document.getElementById("variables-editor-container");

            if (!container) {
                console.warn("Container for editor not present");
            }
            else {
                this.#editorContainer = container;
            }
        }
        if (!this.#diffEditorContainer) {
            const container = document.getElementById("variables-diff-editor-container");

            if (!container) {
                console.warn("Container for diff editor not present");
            }
            else {
                this.#diffEditorContainer = container;
            }
        }
    }

    /**
     * @param {Object} dotNetRef
     * @param {string} config
     * @param {string} schema
     * @param {boolean} isReadonly
     */
    updateEditor(dotNetRef, config, schema, isReadonly) {
        this.#dotNetRef = dotNetRef;
        this.#disposeDiffEditorModels();
        this.#initializeEditorContainers();
        this.#diffEditorContainer.style.display = "none";
        this.#editorContainer.style.display = "block";

        if (!this.#modelUri) {
            this.#modelUri = monaco.Uri.parse("internal://server/config.json");
        }

        if (this.#schema !== schema) {
            this.#schema = schema;
            monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
                validate: true,
                allowComments: false,
                schemas: [{
                    // If we give the model a name that matches wich this filematch
                    // monaco will use this schema file for validation
                    uri: "http://server/config-schema",
                    fileMatch: [this.#modelUri.toString()],
                    schema: JSON.parse(schema),
                }],
                enableSchemaRequest: false
            });
        }

        if (!this.#editor) {
            monaco.editor.setTheme("vs-dark");

            this.#model = monaco.editor.createModel(config, "json", this.#modelUri);
            this.#editor = monaco.editor.create(this.#editorContainer, {
                model: this.#model,
                automaticLayout: true,
                readonly: isReadonly
            });

            this.#editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, async () => {
                await this.save();
            });
        } else {
            this.#model?.dispose();
            this.#model = monaco.editor.createModel(config, "json", monaco.Uri.parse("internal://server/config.json"));
            this.#editor.setModel(this.#model);
            this.#editor.updateOptions({ readOnly: isReadonly });
            this.#editorContainer.style.display = "block";
        }

        this.#editor.layout({
            height: document.documentElement.clientHeight - this.#editorContainer.offsetTop,
            width: this.#editorContainer.clientWidth
        });
    }

    /**
     * @param {Object} dotNetRef
     * @param {string} originalConfig
     * @param {string} modifiedConfig
     * @param {string} schema
     */
    updateDiffEditor(dotNetRef, originalConfig, modifiedConfig, schema) {
        this.#dotNetRef = dotNetRef;
        this.#disposeEditorModels();
        this.#initializeEditorContainers();
        this.#editorContainer.style.display = "none";
        this.#diffEditorContainer.style.display = "block";
        
        if (!this.#originalModelUri || !this.#modifiedModelUri) {
            this.#originalModelUri = monaco.Uri.parse("internal://server/original-config.json");
            this.#modifiedModelUri = monaco.Uri.parse("internal://server/modified-config.json");
        }

        if (this.#diffSchema !== schema) {
            this.#diffSchema = schema;
            monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
                validate: true,
                allowComments: false,
                schemas: [{
                    // If we give the model a name that matches wich this filematch
                    // monaco will use this schema file for validation
                    uri: "http://server/config-schema",
                    fileMatch: [this.#originalModelUri.toString(), this.#modifiedModelUri.toString()],
                    schema: JSON.parse(schema),
                }],
                enableSchemaRequest: false
            });
        }

        if (!this.#diffEditor) {
            monaco.editor.setTheme("vs-dark");

            this.#originalModel = monaco.editor.createModel(originalConfig, "json", this.#originalModelUri);
            this.#modifiedModel = monaco.editor.createModel(modifiedConfig, "json", this.#modifiedModelUri);

            this.#diffEditor = monaco.editor.createDiffEditor(this.#diffEditorContainer, {
                automaticLayout: false,
                readOnly: true,
                enableSplitViewResizing: false,
                renderSideBySide: true
            });

            this.#diffEditor.setModel({
                original: this.#originalModel,
                modified: this.#modifiedModel
            });
        } else {
            this.#originalModel?.dispose();
            this.#modifiedModel?.dispose();
            this.#originalModel = monaco.editor.createModel(originalConfig, "json", this.#originalModelUri);
            this.#modifiedModel = monaco.editor.createModel(modifiedConfig, "json", this.#modifiedModelUri);
            this.#diffEditor.setModel({
                original: this.#originalModel,
                modified: this.#modifiedModel
            });
        }

        this.#diffEditor.layout({
            height: document.documentElement.clientHeight - this.#diffEditorContainer.offsetTop,
            width: this.#diffEditorContainer.clientWidth
        });
    }

    destroy() {
        this.#disposeEditorModels();
        this.#disposeDiffEditorModels();
        this.#editor?.dispose();
        this.#diffEditor?.dispose();
        this.#editor = null;
        this.#diffEditor = null;
        this.#dotNetRef = null;
        this.#editorContainer = null;
        this.#diffEditorContainer = null;
    }

    async save() {
        const config = this.#editor.getValue();
        await this.#dotNetRef.invokeMethodAsync("Save", config);
    }
}

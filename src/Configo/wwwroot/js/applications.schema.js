// use strict
((global) => {
    const state = {};
    
    let initializeEditor = () => {
        if(state.editor) {
            return;
        }

        const container = document.getElementById("editor-container");
        state.editor = global.monaco.editor.create(container, {
            value: ['function x() {', '\tconsole.log("Hello world!");', '}'].join('\n'),
            language: 'javascript'
        });

        // TODO other stuff
    }

    global.applications = global.applications ?? {};
    global.applications.schema = {
        initializeEditor: () => {}/*window.require(["vs/editor/editor.main"], () => initializeEditor())*/
    };
})(window);


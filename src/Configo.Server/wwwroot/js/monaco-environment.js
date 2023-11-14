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

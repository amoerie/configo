//@ts-check

import chalk from 'chalk';
import esbuild from 'esbuild';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

(async () => {
    // Force chalk to use colors, even though runAll creates virtual output pipes
    process.env["FORCE_COLOR"] = "1";

    const __filename = fileURLToPath(import.meta.url);
    const __dirname = dirname(__filename);
    const root = path.resolve(__dirname);

    console.log("");
    console.log(chalk.grey(`************************************************************`));
    console.log(chalk.grey(`Starting build`));
    console.log(chalk.grey(`************************************************************`));
    console.log("");

    try {
        const workerEntryPoints = [
            'vs/language/json/json.worker.js',
            'vs/language/css/css.worker.js',
            'vs/language/html/html.worker.js',
            'vs/language/typescript/ts.worker.js',
            'vs/editor/editor.worker.js'
        ];

        build({
            entryPoints: workerEntryPoints.map((entry) => `node_modules/monaco-editor/esm/${entry}`),
            bundle: true,
            format: 'iife',
            platform: "browser",
            outbase: 'node_modules/monaco-editor/esm/',
            outdir: path.join(root, "wwwroot/dist")
        });

        build({
            entryPoints: ['wwwroot/js/applications.schema.js'],
            bundle: true,
            platform: "browser",
            format: 'iife',
            outdir: path.join(root, 'wwwroot/dist'),
            loader: {
                '.ttf': 'file'
            }
        });

        console.log("");
        console.log(chalk.green(`************************************************************`));
        console.log(chalk.green(`Build succeeded \\o/`));
        console.log(chalk.green(`************************************************************`));
        console.log("");

    } catch (error) {
        console.log("");
        console.error(chalk.red(`************************************************************`));
        console.error(chalk.red(`Build failed ;_;\n`), error);
        console.error(chalk.red(`************************************************************`));
        console.log("");
    }

    /**
     * @param {import ('esbuild').BuildOptions} opts
     */
    function build(opts) {
        esbuild.build(opts).then((result) => {
            if (result.errors.length > 0) {
                console.error(result.errors);
            }
            if (result.warnings.length > 0) {
                console.error(result.warnings);
            }
        });
    }
})();


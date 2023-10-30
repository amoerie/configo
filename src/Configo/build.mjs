//@ts-check

import chalk from 'chalk';
import esbuild from 'esbuild';
import path from 'path';
import { fileURLToPath } from 'url';
import { dirname } from 'path';

(async () => {
    // Force chalk to use colors, even though runAll creates virtual output pipes
    process.env["FORCE_COLOR"] = "1";

    const args = process.argv.slice(2);

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


        const workerOptions = {
            entryPoints: workerEntryPoints.map((entry) => `node_modules/monaco-editor/esm/${ entry }`),
            bundle: true,
            format: 'iife',
            platform: "browser",
            outbase: 'node_modules/monaco-editor/esm/',
            outdir: path.join(root, "wwwroot/dist")
        };

        const pageOptions = {
            entryPoints: ['wwwroot/js/applications.schema.js'],
            bundle: true,
            platform: "browser",
            format: 'iife',
            outdir: path.join(root, 'wwwroot/dist'),
            loader: {
                '.ttf': 'file'
            }
        };

        if (args.includes("--watch")) {
            await Promise.all([watch(workerOptions), watch(pageOptions)]);
        } else {
            await Promise.all([build(workerOptions), build(pageOptions)]);
        }

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
    async function build(opts) {
        const result = await esbuild.build(opts);
        if (result.errors.length > 0) {
            console.error(result.errors);
        }
        if (result.warnings.length > 0) {
            console.error(result.warnings);
        }
    }

    /**
     * @param {import ('esbuild').BuildOptions} opts
     */
    async function watch(opts) {
        /**
         * @type {import ('esbuild').Plugin}
         */
        const onEndPlugin = {
            name: 'on-end',
            setup(build) {
                build.onEnd((result) => {
                    const now = new Date();
                    const time = `${ now.getHours().toString().padStart(2, "0") }:${ now.getMinutes().toString().padStart(2, "0") }:${ now.getSeconds().toString().padStart(2, "0") }.${ now.getMilliseconds().toString().padStart(3, "0") }`;
                    if (result.errors && result.errors.length > 0) {
                        for (const error of result.errors) {
                            console.error(chalk.red(`${ time } ❌ Rebuild failed: ${ error }`), error);
                        }
                    }
                    else if (result.warnings && result.warnings.length > 0) {
                        for (const warning of result.warnings) {
                            console.error(chalk.yellow(`${ time } ⚠️ Rebuild warning: ${ warning}`));
                        }
                    }
                    else {
                       console.info(chalk.green(`${ time } ✅ Rebuild successful`));
                    }
                });
            },
        };
        opts.plugins = opts.plugins ?? [];
        opts.plugins.push(onEndPlugin);
        const context = await esbuild.context(opts);
        await context.watch();
    }
})();


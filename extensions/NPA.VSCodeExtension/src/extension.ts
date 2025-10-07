import * as vscode from 'vscode';

export function activate(context: vscode.ExtensionContext) {
    console.log('NPA VSCode Extension is now active!');

    // Register command for generating repositories
    let generateRepoCommand = vscode.commands.registerCommand('npa.generateRepository', async () => {
        const activeEditor = vscode.window.activeTextEditor;
        if (!activeEditor) {
            vscode.window.showErrorMessage('No active editor found');
            return;
        }

        const document = activeEditor.document;
        if (document.languageId !== 'csharp') {
            vscode.window.showErrorMessage('This command only works with C# files');
            return;
        }

        // TODO: Implement repository generation logic
        vscode.window.showInformationMessage('Repository generation feature coming soon!');
    });

    // Register command for scaffolding entities
    let scaffoldCommand = vscode.commands.registerCommand('npa.scaffoldEntities', async () => {
        const connectionString = await vscode.window.showInputBox({
            prompt: 'Enter database connection string',
            placeHolder: 'Server=.;Database=MyDb;Integrated Security=true;'
        });

        if (!connectionString) {
            return;
        }

        // TODO: Implement entity scaffolding logic
        vscode.window.showInformationMessage('Entity scaffolding feature coming soon!');
    });

    context.subscriptions.push(generateRepoCommand, scaffoldCommand);
}

export function deactivate() {
    console.log('NPA VSCode Extension is now deactivated.');
}
# Phase 6.1: VS Code Extension

## 📋 Task Overview

**Objective**: Create a VS Code extension that provides IntelliSense support, code generation tools, and snippets for the NPA library.

**Priority**: Low  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.7, Phase 5.1-5.5 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] VS Code extension project is created
- [ ] IntelliSense support works
- [ ] Code snippets are functional
- [ ] Commands are implemented
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. VS Code Extension Features
- **IntelliSense Support**: Auto-completion for NPA attributes and methods
- **Code Snippets**: Quick code generation for common NPA patterns
- **Commands**: Generate repositories, entities, and migrations
- **Syntax Highlighting**: Highlight NPA-specific syntax
- **Error Detection**: Detect and highlight NPA-related errors
- **Code Actions**: Refactoring and quick fixes

### 2. IntelliSense Support
- **Attribute Completion**: Auto-complete NPA attributes
- **Method Completion**: Auto-complete repository methods
- **Parameter Completion**: Auto-complete method parameters
- **Documentation**: Show documentation for NPA types
- **Type Information**: Display type information on hover

### 3. Code Snippets
- **Entity Snippet**: Generate entity class template
- **Repository Snippet**: Generate repository interface template
- **Query Snippet**: Generate query method template
- **Configuration Snippet**: Generate NPA configuration template

### 4. Commands
- **Generate Repository**: Generate repository from entity
- **Generate Entity**: Generate entity from database table
- **Generate Migration**: Generate migration file
- **Initialize NPA**: Initialize NPA in current project

### 5. VS Code Integration
- **Status Bar**: Show NPA status information
- **Output Channel**: Display NPA build and generation output
- **Problem Matcher**: Detect NPA-specific errors and warnings
- **Settings**: Extension configuration options

## 🏗️ Implementation Plan

### Step 1: Create Extension Project
1. Create VS Code extension project using Yeoman
2. Set up project structure
3. Configure package.json
4. Add necessary dependencies

### Step 2: Implement IntelliSense Support
1. Create completion provider
2. Implement attribute completion
3. Implement method completion
4. Add hover documentation support

### Step 3: Create Code Snippets
1. Define snippet structure
2. Implement entity snippets
3. Implement repository snippets
4. Implement query snippets

### Step 4: Implement Commands
1. Create command infrastructure
2. Implement repository generator command
3. Implement entity generator command
4. Implement migration generator command

### Step 5: Add VS Code Integration
1. Implement status bar integration
2. Add output channel
3. Implement problem matcher
4. Add configuration settings

### Step 6: Create Unit Tests
1. Test IntelliSense functionality
2. Test code snippets
3. Test commands
4. Test VS Code integration

### Step 7: Add Documentation
1. Extension README
2. Usage examples
3. Configuration guide
4. Best practices

## 📁 File Structure

```
extensions/NPA.VSCodeExtension/
├── package.json
├── tsconfig.json
├── .vscodeignore
├── src/
│   ├── extension.ts
│   ├── commands/
│   │   ├── generateRepository.ts
│   │   ├── generateEntity.ts
│   │   └── generateMigration.ts
│   ├── providers/
│   │   ├── completionProvider.ts
│   │   ├── hoverProvider.ts
│   │   └── codeActionProvider.ts
│   ├── snippets/
│   │   ├── entity.json
│   │   ├── repository.json
│   │   └── query.json
│   └── utils/
│       ├── codeGenerator.ts
│       └── templateEngine.ts
└── test/
    ├── extension.test.ts
    ├── commands/
    │   ├── generateRepository.test.ts
    │   ├── generateEntity.test.ts
    │   └── generateMigration.test.ts
    └── providers/
        ├── completionProvider.test.ts
        └── hoverProvider.test.ts
```

## 💻 Code Examples

### Extension Activation
```typescript
import * as vscode from 'vscode';
import { NPACompletionProvider } from './providers/completionProvider';
import { NPAHoverProvider } from './providers/hoverProvider';
import { generateRepository } from './commands/generateRepository';
import { generateEntity } from './commands/generateEntity';

export function activate(context: vscode.ExtensionContext) {
    // Register completion provider
    const completionProvider = vscode.languages.registerCompletionItemProvider(
        'csharp',
        new NPACompletionProvider(),
        '[', '.'
    );
    
    // Register hover provider
    const hoverProvider = vscode.languages.registerHoverProvider(
        'csharp',
        new NPAHoverProvider()
    );
    
    // Register commands
    const generateRepoCommand = vscode.commands.registerCommand(
        'npa.generateRepository',
        generateRepository
    );
    
    const generateEntityCommand = vscode.commands.registerCommand(
        'npa.generateEntity',
        generateEntity
    );
    
    context.subscriptions.push(
        completionProvider,
        hoverProvider,
        generateRepoCommand,
        generateEntityCommand
    );
}

export function deactivate() {}
```

### Completion Provider
```typescript
import * as vscode from 'vscode';

export class NPACompletionProvider implements vscode.CompletionItemProvider {
    provideCompletionItems(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken,
        context: vscode.CompletionContext
    ): vscode.ProviderResult<vscode.CompletionItem[]> {
        const linePrefix = document.lineAt(position).text.substr(0, position.character);
        
        if (this.isInAttributeContext(linePrefix)) {
            return this.getAttributeCompletions();
        }
        
        if (this.isInMethodContext(linePrefix)) {
            return this.getMethodCompletions();
        }
        
        return [];
    }
    
    private isInAttributeContext(linePrefix: string): boolean {
        return linePrefix.includes('[') && !linePrefix.includes(']');
    }
    
    private isInMethodContext(linePrefix: string): boolean {
        return linePrefix.includes('Task<') || linePrefix.includes('async');
    }
    
    private getAttributeCompletions(): vscode.CompletionItem[] {
        const items: vscode.CompletionItem[] = [];
        
        const entityAttr = new vscode.CompletionItem('Entity', vscode.CompletionItemKind.Class);
        entityAttr.detail = 'NPA Entity Attribute';
        entityAttr.documentation = new vscode.MarkdownString('Marks a class as an NPA entity');
        items.push(entityAttr);
        
        const tableAttr = new vscode.CompletionItem('Table', vscode.CompletionItemKind.Class);
        tableAttr.detail = 'NPA Table Attribute';
        tableAttr.documentation = new vscode.MarkdownString('Specifies the database table name');
        items.push(tableAttr);
        
        const idAttr = new vscode.CompletionItem('Id', vscode.CompletionItemKind.Class);
        idAttr.detail = 'NPA Id Attribute';
        idAttr.documentation = new vscode.MarkdownString('Marks a property as the primary key');
        items.push(idAttr);
        
        const columnAttr = new vscode.CompletionItem('Column', vscode.CompletionItemKind.Class);
        columnAttr.detail = 'NPA Column Attribute';
        columnAttr.documentation = new vscode.MarkdownString('Maps a property to a database column');
        items.push(columnAttr);
        
        return items;
    }
    
    private getMethodCompletions(): vscode.CompletionItem[] {
        const items: vscode.CompletionItem[] = [];
        
        const findBy = new vscode.CompletionItem('FindBy', vscode.CompletionItemKind.Method);
        findBy.detail = 'Repository Method';
        findBy.documentation = new vscode.MarkdownString('Find entity by property');
        items.push(findBy);
        
        const findAll = new vscode.CompletionItem('FindAll', vscode.CompletionItemKind.Method);
        findAll.detail = 'Repository Method';
        findAll.documentation = new vscode.MarkdownString('Find all entities');
        items.push(findAll);
        
        return items;
    }
}
```

### Hover Provider
```typescript
import * as vscode from 'vscode';

export class NPAHoverProvider implements vscode.HoverProvider {
    provideHover(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.Hover> {
        const wordRange = document.getWordRangeAtPosition(position);
        if (!wordRange) {
            return null;
        }
        
        const word = document.getText(wordRange);
        const documentation = this.getDocumentation(word);
        
        if (documentation) {
            return new vscode.Hover(new vscode.MarkdownString(documentation));
        }
        
        return null;
    }
    
    private getDocumentation(word: string): string | null {
        const docs: Record<string, string> = {
            'Entity': '**[Entity]** - Marks a class as an NPA entity that maps to a database table.',
            'Table': '**[Table]** - Specifies the database table name for an entity.',
            'Id': '**[Id]** - Marks a property as the primary key of an entity.',
            'Column': '**[Column]** - Maps a property to a database column.',
            'Repository': '**[Repository]** - Marks an interface as an NPA repository.'
        };
        
        return docs[word] || null;
    }
}
```

### Generate Repository Command
```typescript
import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

export async function generateRepository() {
    const entityName = await vscode.window.showInputBox({
        prompt: 'Enter entity name',
        placeHolder: 'User'
    });
    
    if (!entityName) {
        return;
    }
    
    const namespace = await vscode.window.showInputBox({
        prompt: 'Enter namespace',
        placeHolder: 'MyApp.Repositories'
    });
    
    if (!namespace) {
        return;
    }
    
    const repositoryInterface = generateRepositoryInterface(entityName, namespace);
    const fileName = `I${entityName}Repository.cs`;
    
    const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
    if (!workspaceFolder) {
        vscode.window.showErrorMessage('No workspace folder open');
        return;
    }
    
    const filePath = path.join(workspaceFolder.uri.fsPath, fileName);
    fs.writeFileSync(filePath, repositoryInterface);
    
    const document = await vscode.workspace.openTextDocument(filePath);
    await vscode.window.showTextDocument(document);
    
    vscode.window.showInformationMessage(`Repository ${entityName} generated successfully!`);
}

function generateRepositoryInterface(entityName: string, namespace: string): string {
    return `using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core;

namespace ${namespace}
{
    public interface I${entityName}Repository : IRepository<${entityName}, long>
    {
        Task<${entityName}> FindByIdAsync(long id);
        Task<IEnumerable<${entityName}>> FindAllAsync();
        Task<int> CountAsync();
    }
}`;
}
```

## 🧪 Test Cases

### IntelliSense Tests
- [ ] Attribute completion works
- [ ] Method completion works
- [ ] Parameter completion works
- [ ] Hover documentation displays
- [ ] Type information shows correctly

### Snippet Tests
- [ ] Entity snippet generates correctly
- [ ] Repository snippet generates correctly
- [ ] Query snippet generates correctly
- [ ] Configuration snippet generates correctly

### Command Tests
- [ ] Generate repository command works
- [ ] Generate entity command works
- [ ] Generate migration command works
- [ ] Error handling works correctly

### VS Code Integration Tests
- [ ] Status bar displays correctly
- [ ] Output channel works
- [ ] Problem matcher detects errors
- [ ] Settings configuration works

## 📚 Documentation Requirements

### Extension README
- [ ] Features overview
- [ ] Installation instructions
- [ ] Usage examples
- [ ] Configuration guide
- [ ] Troubleshooting

### Usage Guide
- [ ] IntelliSense usage
- [ ] Snippet usage
- [ ] Command usage
- [ ] Configuration options
- [ ] Best practices

## 🔍 Code Review Checklist

- [ ] Code follows TypeScript conventions
- [ ] All functions have documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized

## 🚀 Next Steps

After completing this task:
1. Move to Phase 6.2: Code Generation Tools
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on VS Code integration
- [ ] Performance considerations for IntelliSense
- [ ] Integration with existing features
- [ ] Extension marketplace publishing

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: Planned*

// Monaco Editor Interop for Blazor
window.MonacoInterop = {
    editors: {},

    initialize: function (elementId, options) {
        return new Promise((resolve) => {
            require.config({ paths: { 'vs': 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' } });
            
            require(['vs/editor/editor.main'], function () {
                // Define custom theme
                monaco.editor.defineTheme('MokitDark', {
                    base: 'vs-dark',
                    inherit: true,
                    rules: [
                        { token: 'string.key.json', foreground: '9CDCFE' },
                        { token: 'string.value.json', foreground: 'CE9178' },
                        { token: 'number', foreground: 'B5CEA8' },
                        { token: 'keyword.json', foreground: '569CD6' },
                    ],
                    colors: {
                        'editor.background': '#1a1b26',
                        'editor.foreground': '#c0caf5',
                        'editor.lineHighlightBackground': '#292e42',
                        'editor.selectionBackground': '#364a82',
                        'editorCursor.foreground': '#c0caf5',
                        'editorLineNumber.foreground': '#565f89',
                        'editor.inactiveSelectionBackground': '#292e4266',
                    }
                });

                const element = document.getElementById(elementId);
                if (!element) {
                    console.error('Monaco: Element not found:', elementId);
                    resolve(false);
                    return;
                }

                const defaultOptions = {
                    value: options.value || '{}',
                    language: options.language || 'json',
                    theme: 'MokitDark',
                    automaticLayout: true,
                    minimap: { enabled: false },
                    scrollBeyondLastLine: false,
                    fontSize: 14,
                    fontFamily: "'Fira Code', 'Monaco', 'Consolas', monospace",
                    lineNumbers: 'on',
                    roundedSelection: true,
                    scrollbar: {
                        vertical: 'auto',
                        horizontal: 'auto',
                        useShadows: false,
                        verticalScrollbarSize: 8,
                        horizontalScrollbarSize: 8
                    },
                    padding: { top: 10, bottom: 10 },
                    folding: true,
                    bracketPairColorization: { enabled: true },
                    formatOnPaste: true,
                    formatOnType: true,
                    tabSize: 2,
                    wordWrap: 'on',
                    suggest: {
                        showWords: false
                    }
                };

                const editor = monaco.editor.create(element, defaultOptions);
                window.MonacoInterop.editors[elementId] = editor;

                // Register custom completions for Mokit variables
                if (options.language === 'json') {
                    monaco.languages.registerCompletionItemProvider('json', {
                        triggerCharacters: ['{', '.'],
                        provideCompletionItems: function (model, position) {
                            const textUntilPosition = model.getValueInRange({
                                startLineNumber: position.lineNumber,
                                startColumn: 1,
                                endLineNumber: position.lineNumber,
                                endColumn: position.column
                            });

                            // Check if we're inside {{ }}
                            const lastOpen = textUntilPosition.lastIndexOf('{{');
                            const lastClose = textUntilPosition.lastIndexOf('}}');
                            
                            if (lastOpen > lastClose || textUntilPosition.endsWith('{{')) {
                                return {
                                    suggestions: window.MonacoInterop.getVariableSuggestions(position)
                                };
                            }

                            return { suggestions: [] };
                        }
                    });
                }

                resolve(true);
            });
        });
    },

    getValue: function (elementId) {
        const editor = window.MonacoInterop.editors[elementId];
        return editor ? editor.getValue() : '';
    },

    setValue: function (elementId, value) {
        const editor = window.MonacoInterop.editors[elementId];
        if (editor) {
            editor.setValue(value || '');
        }
    },

    formatDocument: function (elementId, language) {
        const editor = window.MonacoInterop.editors[elementId];
        if (!editor) return;
        
        const value = editor.getValue();
        let formatted = value;
        
        try {
            if (language === 'json') {
                // JSON formatting
                const parsed = JSON.parse(value);
                formatted = JSON.stringify(parsed, null, 2);
            } else if (language === 'xml') {
                // Simple XML formatting
                formatted = this.formatXml(value);
            } else if (language === 'html') {
                // Simple HTML formatting (same as XML)
                formatted = this.formatXml(value);
            }
            // For other languages, try Monaco's built-in formatter
            else {
                const action = editor.getAction('editor.action.formatDocument');
                if (action) {
                    action.run();
                    return;
                }
            }
            
            if (formatted !== value) {
                editor.setValue(formatted);
            }
        } catch (e) {
            console.warn('Format error:', e.message);
            // If parsing fails, try Monaco's built-in formatter
            const action = editor.getAction('editor.action.formatDocument');
            if (action) action.run();
        }
    },

    formatXml: function (xml) {
        const PADDING = '  ';
        let formatted = '';
        let pad = 0;
        
        // Remove existing whitespace between tags
        xml = xml.replace(/(>)(<)(\/*)/g, '$1\n$2$3');
        
        xml.split('\n').forEach(function(node) {
            let indent = 0;
            if (node.match(/.+<\/\w[^>]*>$/)) {
                // Node with opening and closing tag on same line
                indent = 0;
            } else if (node.match(/^<\/\w/)) {
                // Closing tag
                if (pad !== 0) pad -= 1;
            } else if (node.match(/^<\w([^>]*[^\/])?>.*$/)) {
                // Opening tag (not self-closing)
                indent = 1;
            } else {
                indent = 0;
            }
            
            formatted += PADDING.repeat(pad) + node.trim() + '\n';
            pad += indent;
        });
        
        return formatted.trim();
    },

    dispose: function (elementId) {
        const editor = window.MonacoInterop.editors[elementId];
        if (editor) {
            editor.dispose();
            delete window.MonacoInterop.editors[elementId];
        }
    },

    setLanguage: function (elementId, language) {
        const editor = window.MonacoInterop.editors[elementId];
        if (editor) {
            monaco.editor.setModelLanguage(editor.getModel(), language);
        }
    },

    onContentChanged: function (elementId, dotNetHelper) {
        const editor = window.MonacoInterop.editors[elementId];
        if (editor) {
            editor.onDidChangeModelContent(function () {
                dotNetHelper.invokeMethodAsync('OnContentChanged', editor.getValue());
            });
        }
    },

    insertText: function (elementId, text) {
        const editor = window.MonacoInterop.editors[elementId];
        if (editor) {
            const selection = editor.getSelection();
            const id = { major: 1, minor: 1 };
            const op = { identifier: id, range: selection, text: text, forceMoveMarkers: true };
            editor.executeEdits("custom", [op]);
        }
    },

    getVariableSuggestions: function (position) {
        const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: position.column,
            endColumn: position.column
        };

        return [
            // Faker - Name
            { label: 'faker.name.fullName', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.name.fullName}}', detail: 'Tam isim', range: range },
            { label: 'faker.name.firstName', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.name.firstName}}', detail: 'Ad', range: range },
            { label: 'faker.name.lastName', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.name.lastName}}', detail: 'Soyad', range: range },
            { label: 'faker.internet.email', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.internet.email}}', detail: 'E-posta', range: range },
            { label: 'faker.internet.userName', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.internet.userName}}', detail: 'Kullanıcı adı', range: range },
            { label: 'faker.phone.number', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.phone.number}}', detail: 'Telefon', range: range },
            
            // Faker - Random
            { label: 'faker.random.uuid', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.random.uuid}}', detail: 'UUID', range: range },
            { label: 'faker.random.number', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.random.number}}', detail: 'Rastgele sayı', range: range },
            { label: 'faker.random.boolean', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.random.boolean}}', detail: 'Boolean', range: range },
            
            // Faker - Commerce
            { label: 'faker.commerce.productName', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.commerce.productName}}', detail: 'Ürün adı', range: range },
            { label: 'faker.commerce.price', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.commerce.price}}', detail: 'Fiyat', range: range },
            
            // Faker - Date
            { label: 'faker.date.recent', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.date.recent}}', detail: 'Yakın tarih', range: range },
            { label: 'faker.date.past', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.date.past}}', detail: 'Geçmiş tarih', range: range },
            
            // Faker - Address
            { label: 'faker.address.city', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.address.city}}', detail: 'Şehir', range: range },
            { label: 'faker.address.country', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.address.country}}', detail: 'Ülke', range: range },
            
            // Faker - Image
            { label: 'faker.image.avatar', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'faker.image.avatar}}', detail: 'Avatar URL', range: range },
            
            // Simple variables
            { label: 'now', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'now}}', detail: 'Şu anki zaman (ISO)', range: range },
            { label: 'uuid', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'uuid}}', detail: 'Yeni UUID', range: range },
            { label: 'today', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'today}}', detail: 'Bugünün tarihi', range: range },
            
            // Request
            { label: 'request.params.id', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'request.params.id}}', detail: 'URL parametresi', range: range },
            { label: 'request.query.page', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'request.query.page}}', detail: 'Query string', range: range },
            { label: 'request.body.name', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'request.body.name}}', detail: 'Request body alanı', range: range },
            { label: 'request.id', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'request.id}}', detail: 'Request ID', range: range },
            { label: 'request.method', kind: monaco.languages.CompletionItemKind.Variable, insertText: 'request.method}}', detail: 'HTTP metodu', range: range },
        ];
    }
};


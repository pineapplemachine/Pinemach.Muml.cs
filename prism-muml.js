/**
 * Muml syntax highlighting definition for Prism.
 */

Prism.languages.muml = {
    'comment': {
        pattern: /(?:(#{3,}).+?\1)|(?:#\[.*?#\])|(?:#[^\n]*)/s,
    },
    'text': {
        pattern: /\|($|[ \t].*)/,
    },
    'string': {
        pattern: /(?:('{3,}|"{3,})(?:\\.|.)+?\1)|(?:(`{3,}).+?\2)|'(?:\\.|[^'\n])*?'|"(?:\\.|[^"\n])*?"|`(?:``|[^`])*?`/s,
    },
    'operator': {
        pattern: /\|[|>;^=][$+*\-]?\.*/,
    },
    'punctuation': {
        pattern: /[()\[\]\{\};,&=]/,
    },
};

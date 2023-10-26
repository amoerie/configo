window.clipboardHelper = {
    copyText: function(text) {
        navigator.clipboard.writeText(text)
            .then(function() { alert("OK"); }).catch();
    }
};

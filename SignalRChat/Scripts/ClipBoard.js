function ClipBoard(pasteCallBack) {

        var hiddenInput = $('<input id="hidden-input" class="hidden" type="text" value="" />')
        .css({
                position: 'absolute',
                bottom: 0,
                left: 0,
                width: '10px',
                height: '10px',
                display: 'block',
                'font-size': 1,
                'z-index': -1,
                color: 'transparent',
                background: 'transparent',
                overflow: 'hidden',
                border: 'none',
                padding: 0,
                resize: 'none',
                outline: 'none',
                '-webkit-user-select': 'text',
                'user-select': 'text'
        })
        .appendTo($('body'));

        var ctrlPressed = false;
        var focused;

        // Catch 'Ctrl + V' to focus on hidden element
        document.addEventListener('keydown', function (e) {
                if (e.keyCode == 17) {
                        ctrlPressed = true;
                }
                if (ctrlPressed && e.keyCode == 86) {
                        focused = $(':focus');
                        Echo("FOCUSED", focused.attr('id'));
                        hiddenInput.focus().select();
                }
        });

        document.addEventListener('keyup', function (e) {
                if (e.keyCode == 17) ctrlPressed = false;
        });

        // Set clipboard event listeners on the document. 
        ['cut', 'copy', 'paste'].forEach(function (event) {
                document.addEventListener(event, function (e) {
                        setTimeout(function () {
                                if (event == 'paste') {
                                        pasteCallBack(hiddenInput.val());
                                        focused.focus();
                                }
                        }, 100);
                });
        });
}
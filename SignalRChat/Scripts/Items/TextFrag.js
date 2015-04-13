



function TextFrag(parent, params) {
        params.id = params.id || 'text' + Now();

        Echo("id ", params.id);

        params.left = Math.floor(params.left); // TODO: collect this flooring shit together
        params.top = Math.floor(params.top);

        var textFrag = $('#' + params.id);
        if (textFrag.length > 0) {
                textFrag.trigger('UpdTextParams', params);
                return textFrag;
        }

        Echo('TextFrag ', params);

        textFrag = $('<span id=' + params.id + '></span>')
               .data({ params: params })
               .css({
                    'position': 'absolute',
                    'font-size': '30px',
                    'color': '#333333',
                    'min-width': '1px'
               })
               .bind('UpdTextParams', function (e, newParams) {
                       Echo('UpdTextParams triggered', newParams);

                       var p = textFrag.data('params');
                       $.extend(p, newParams);
                       textFrag
                             .css({
                                     'top': p.top + 'px',
                                     'left': p.left + 'px',
                             });
                       p.maxWidth = Math.floor(p.maxWidth || parent.width() - p.left - 2);
                       p.maxHeight = Math.floor(p.maxHeight || parent.height() - p.top - 1);

                       p.value = p.value || '';

                       textFrag.htmlValue();
               });

        textFrag
            .mouseover(function () { parent.SetActiveTextFrag(textFrag); })
            .mouseout(function () { textFrag.Deactivate(); })
            .click(function () { parent.SetActiveTextFrag(textFrag); return false; })
            .mousemove(function() { Echo("TESXTEDIT MOVE!"); return false; })
            .appendTo(parent);

        var textCont = $("<span id=added></span>")
            .appendTo(textFrag);

        textFrag.Redraw = function () {
                textFrag.htmlValue(); // little trick to forcely redraw the text fragment. 
        }

        textFrag.Activate = function () {
                textFrag.css({ border: '1px #224422 solid' });
                $('#cursor').appendTo(textFrag);
                parent.activeTextFrag = textFrag;

                fridge.SetActiveSquare(parent);

                textFrag.Redraw(); // need it?
        }

        textFrag.Deactivate = function () {
                textFrag.css({ border: 'none' });
                $('#cursor').remove();
        }

        textFrag.htmlValue = function (fontSize) {
                fontSize = (fontSize === undefined ? 30 : fontSize);

                textFrag.css('font-size', fontSize + 'px');
                textCont.css('font-size', fontSize + 'px');

                params.type = params.type || 'text';

                if (params.type != 'link')
                        textCont.html(escapeHTML(params.value));
                else
                        textCont.html($('<a></a>').attr('href', params.value).text('link')); 

                cursor.appendTo(textFrag);
         
                Echo(textFrag.width(), params.maxWidth, textFrag.height(), params.maxHeight);
                if (textFrag.width() > params.maxWidth || textFrag.height() > params.maxHeight) {
                        textFrag.htmlValue(Math.floor(fontSize / 1.2));
                }
        }


        textFrag.keyPress = function (e) {
                var code = e.keyCode;
                var str = textFrag.data('params').value;
                Echo('inkeypress', textFrag.data('params'));

                if (code == 13) {
                        parent.createEdit({
                                top: textFrag.position().top + textFrag.height() + 1,
                                left: textFrag.position().left
                        });
                        return;
                }
                else if (code == 8) {
                        if (str.length > 0) {
                                str = str.substring(0, str.length - 1);
                        }
                }
                else {
                        str = str + String.fromCharCode(code);
                }

                textFrag.data('params').value = str;
                textFrag.htmlValue();

                sendToServer('updSquare', parent);

                return false;
        }

        textFrag.trigger('UpdTextParams', [params]);

        return textFrag;
}
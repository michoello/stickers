
function randomColor() {
        var r = Math.floor(Math.random() * 40) + 215;
        var g = Math.floor(Math.random() * 40) + 215;
        var b = Math.floor(Math.random() * 40) + 215;
        return 'rgb(' + r + ',' + g + ',' + b + ')';
}

function Square(params)
{
        params.id = params.id || 'square' + Now();
        var square = $('#' + params.id);

        if (square.length > 0) {
                square.trigger('UpdParams', [params]); // why params not working?
                return square;
        }

        Echo('addsquare', params);

        square = $('<div class=square id=' + params.id + '></div>')
               .css({
                       'position': 'absolute',
                       //'background-image': 'url("/images/wood.jpg")'
               })
               .appendTo(fridge)
               .bind('UpdParams', function (e, newParams) {
                       Echo('UpdParams triggered', newParams);

                       var p = square.data('params') || {};
                       $.extend(p, newParams);

                       p.left = Math.floor(p.left);
                       p.top = Math.floor(p.top);

                       p.angle = p.angle || 0;
                       p.color = p.color || randomColor();
                       p.texts = p.texts || [];
                       p.size = p.size || 160;

                       //var color1 = randomColor();
                       var color2 = randomColor();
                       var colorGrad = '(' + p.color + ', ' + color2 + ')';
                       var scale = ''; // square.hasClass('squareselected') ? 'scale(2,2)' : 'scale(1,1)'; this GLYUCHIT!

                       square.data('params', p)
                             .css({
                                     'left': p.left + 'px',
                                     'top': p.top + 'px',
                                     'height': p.size + 'px',
                                     'width': p.size + 'px',
                                     'background-color': p.color,

                                     'background': '-webkit-linear-gradient' + colorGrad,  /* For Safari 5.1 to 6.0 */
                                     'background': '-o-linear-gradient' + colorGrad,  /* For Opera 11.1 to 12.0 */
                                     'background': '-moz-linear-gradient' + colorGrad,  /* For Firefox 3.6 to 15 */

                                     'background': 'linear-gradient' + colorGrad,   /* Standard syntax (must be last) */

                                     '-ms-transform': scale + ' rotate(' + p.angle + 'deg)', /* IE 9 */
                                     '-webkit-transform': scale + ' rotate('+ p.angle +'deg)', /* Chrome, Safari, Opera */
                                     'transform': scale + ' rotate(' + p.angle + 'deg)' /* Standard syntax */
                             });
               })
               .bind('UpdTexts', function (e, newParams) {
                   var t = square.data('params').texts;

                   for (var id in t) {
                           Echo('text[', id, '=', t[id]);
                           TextFrag(square, t[id]);
                   }
               });


        square.params = function (paramsValue) {
                if (paramsValue == undefined) {
                        return square.data('params');
                }
                square.trigger('UpdParams', [paramsValue]);
        }

        square.params(params);

        square.createEdit = function (textParams) {
                var textFr = TextFrag(square, textParams);
                square.params().texts.push(textParams);
                square.SetActiveTextFrag(textFr);
        }

        square.activeTextFrag = undefined;
        square.SetActiveTextFrag = function (textFrag) {
                if (square.activeTextFrag != undefined) {
                        square.activeTextFrag.Deactivate();
                }
                if (textFrag != undefined) {
                        square.activeTextFrag = textFrag;
                        textFrag.Activate();
                }
        }

        square
            .mouseover(function () { fridge.SetActiveSquare(square); })
            .mouseout(function () { fridge.SetActiveSquare(undefined); })

        var p = square.data('params');

        var clickTime;
        square.mousedown(function (e) { clicktime = Now(); })
        .mouseup(function (e) {
                Echo('Mouse up!!!');
                if (Now() - clicktime < 500) {
                        square.createEdit({
                                left: Math.floor(e.pageX - square.params().left + 0.01),
                                top: Math.floor(e.pageY - square.params().top + 0.01 /* for firefox */),
                        });
                }
        })

/*        square.click(function (e) {
                square.createEdit({
                        left: Math.floor(e.pageX - square.params().left + 0.01),
                        top: Math.floor(e.pageY - square.params().top + 0.01 /* for firefox ),
                });
                return false;
        });*/

        square.move = function (xdelta, ydelta, dontSend) {
                square.params({
                        'top': square.params().top + ydelta, 
                        'left': square.params().left + xdelta, 
                });
                if (!dontSend) {
                        sendToServer('moveSquare', square);
                }
                return false;
        }

        square.keyPress = function (e) {
                if (square.activeTextFrag != undefined) {

                        var k = e.keyCode;
                        if (k == 40 || k == 38 || k == 37 || k == 39) { // arrows
                                square.move((k == 39 ? 10 : (k == 37 ? -10 : 0)), (k == 40 ? 10 : (k == 38 ? -10 : 0)));
                        }
                        else {
                                return square.activeTextFrag.keyPress(e);
                        }
                }
                return true;
        }

        square.bind('fallSquare', function (e, local) {
                var speed = 0;
                var stepDown = function () {
                        speed = speed + 1;
                        square.move(0, speed, true);

                        if (p.top < 1000) {
                                setTimeout(stepDown, 30);
                        }
                        else {
                                square.remove();
                        }
                };
                setTimeout(stepDown, 50);

                fridge.SetActiveSquare(undefined);

                return false;
        });

        var downButton = Button(square.params().size - 20, 0, 20, 'red');
        downButton
                .click(function (e) {
                        square.trigger('fallSquare');
                        sendToServer('fallSquare', square);
                        return false;
                })
                .appendTo(square);

        var colorButton = Button(square.params().size - 40, 0, 20, 'green');
        colorButton
            .click(function (e) {
                    square.params({ color: '#bbaacc' });
                    return false;
            })
            .appendTo(square);

        var rotateButton = Button(square.params().size - 60, 0, 20, 'yellow');
        rotateButton
                .click(function (e) {
                        square.params({ 'angle': p.angle + 10 });
                        sendToServer('moveSquare', square);
                        return false;
                })
                .appendTo(square);

        return square;
}

// ------------------------------------------------------------------------------------------------------------------------------------------------------------


function Button(left, top, size, color) {
        var button = $('<div></div>')
            .css({
                    'top': top + 'px',
                    'left': left + 'px',
                    'position': 'absolute',
                    height: size + 'px',
                    width: size + 'px',
                    border: 'none',
                    'background-color': color,
                    opacity: 0.1
            })
            .mouseover(function () {
                    $(this).css('opacity', 1);
            })
            .mouseout(function () {
                    $(this).css('opacity', 0.1);
            });

        return button;
}
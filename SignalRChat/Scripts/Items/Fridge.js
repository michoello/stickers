function Fridge() {
        var fridge = $('#fridge').css('border', '#abefcd solid 1px');

        var selectedSquare = undefined;

        fridge.SetActiveSquare = function (square) {
                if (selectedSquare != undefined) {
                        selectedSquare.removeClass('squareselected');
                }

                if (square != undefined) {
                        square.addClass('squareselected');
                }

                selectedSquare = square;
        };

        fridge.keyPress = function (e) {
                if (selectedSquare != undefined) {
                        return selectedSquare.keyPress(e);
                }
                return true;
        }

        fridge.click(function (e) {
                if (selectedSquare === undefined) {
                        var square = fridge.addSquare({ left: e.pageX, top: e.pageY });
                        square.createEdit({ top: 2, left: 2 });

                        fridge.SetActiveSquare(square);

                        sendToServer('addSquare', square);
                }
                else {
                        fridge.SetActiveSquare(undefined);
                }
        });

        chat.client.broadcastSand = function (name, message) {
                var cmd = JSON.parse(message);

                if (cmd.type === 'addSquare') {
                        fridge.addSquare(cmd.data);
                }
                if (cmd.type === 'updSquare') {
                        var square = fridge.addSquare(cmd.data);
                        square.trigger('UpdTexts', [cmd.data]);
                }

                if (cmd.type === 'moveSquare') {
                        fridge.addSquare(cmd.data);
                }

                if (cmd.type == 'fallSquare') {
                        var sq = $('#' + cmd.data.id);
                        if (sq.size() > 0) {
                                sq.trigger('fallSquare', []); 
                        }
                }
        };

        fridge.addSquare = function (params) {
                return Square(params);
        }

        fridge.addSquareWithText = function (text) {
                var sq = fridge.addSquare({
                        left: 200,
                        top: 200,
                });
                sq.createEdit({ top: 2, left: 2, value: text });
        }

        var lastLeft = undefined, lastTop = undefined;
        var speed = 0, prevSpeed = 0;

        fridge
                .mousedown(function (e) {
                        lastLeft = Math.floor(e.pageX);
                        lastTop = Math.floor(e.pageY);
                        speed = prevSpeed = 0;
                })
                .mouseup(function (e) {
                        lastLeft = undefined;
                        if (selectedSquare != undefined) {
                            selectedSquare.trigger('UpdParams', [{ 'angle': 0 }]);
                        }

                })
                .mousemove(function (e) {
                    var left = Math.floor(e.pageX);
                    var top = Math.floor(e.pageY);

                    if (lastLeft != undefined) {
                            if (selectedSquare != undefined) {
                                    selectedSquare.move(left - lastLeft, top - lastTop);

                                    speed = (left - lastLeft) + (top - lastTop); 

                                    var delta = speed - prevSpeed > 2 ? 1 : (prevSpeed - speed > 2 ? -1 : 0); // it should depend on drag point of square

                                    selectedSquare.trigger('UpdParams', [{ 'angle': selectedSquare.data('params').angle + delta }]);
                                    prevSpeed = speed;

                            }

                            //drawLine(lastLeft, lastTop, left, top);
                            lastLeft = left;
                            lastTop = top;
                    }
            })




        return fridge;
}
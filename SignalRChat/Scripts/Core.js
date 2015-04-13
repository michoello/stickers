$.fn.p = function (newValue) {
        this.trigger('UpdParams', newValue);
        return this;
}
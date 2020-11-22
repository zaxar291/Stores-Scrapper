function ReferenceEquals(obj1, obj2, strict = false) {
    if (!strict) return obj1 == obj2;
    return obj1 === obj2;
}

if (!String.prototype.trim) {
    String.prototype.trim = function() {
        return this.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, '');
    }
}
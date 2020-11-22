return TaskBrotherWithGlassGetCollectionsList($);

function TaskBrotherWithGlassGetCollectionsList($) {
    var _l = $(".buddha-menu-item.nav-bar__item .nav-bar__link.link");
    if (ReferenceEquals(_l.length, 0)) {
        return "";
    }
    var _c = [];
    var _e = ["Shop By", "Artist/Brands", "Reviews"];
    for (var l of _l) {
        if (ReferenceEquals(typeof l, "object") && !ReferenceEquals(l, "prevObject")) {
            var _current = $(l).text().trim();
            if (ReferenceEquals(_e.includes(_current), false)) {
                _c.push(_current);
            }
        }
    }
    return _c.join("||||");
}


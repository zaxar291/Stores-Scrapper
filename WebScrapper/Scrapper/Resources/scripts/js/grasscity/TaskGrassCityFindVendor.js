return TaskGrasscityGetVendor(jQuery);

function TaskGrasscityGetVendor($) {
    var rTable = $("#product-attribute-specs-table");
    if (rTable.length === 0) return "";

    var rows = $(rTable.find("tr"));
    if (rows.length === 0) return "";

    for (var row of rows) {
        var currentCellHeader = $(row).find("th").text();
        if (ReferenceEquals(currentCellHeader, "Brand")) {
            return $(row).find("td").text();
        }
    }
    return "Grasscity";
}

function ReferenceEquals(o1, o2, strict = false) {
    if (strict) return o1 === o2;
    return o1 == o2;
}
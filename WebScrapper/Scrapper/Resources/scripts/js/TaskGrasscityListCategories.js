return GetSourceList(jQuery);

function GetSourceList(jQuery) {
    $ = jQuery;
    var result_list = [];
    var exclude_rules = ["OUTLET", "New Items", "Shop by Brand"];
    var navigationFirstLevel = $(".v-navigation.v-navigation--mega li.v-navigation__item--level0");
    if (navigationFirstLevel.length > 0) {
        for (var i in navigationFirstLevel) {
            if (typeof navigationFirstLevel[i] === "object" && i !== "prevObject") {
                var currentFirstLevel = navigationFirstLevel[i];
                var firstLevelText = $(currentFirstLevel).find("a.v-navigation__link--level0 span.v-navigation__text-wrapper");
                if (firstLevelText.length > 0) {
                    var currentCat = $(firstLevelText.first()).text();
                    if (!exclude_rules.includes(currentCat)) {
                        result_list.push($(firstLevelText.first()).text());
                    }
                }
                var nextLevel = $(currentFirstLevel).find("ul.v-navigation__list--level0");
                if (nextLevel !== null && nextLevel.length > 0) {
                    var nextLevelItems = $(nextLevel[0]).find("li.v-navigation__item--level1");
                    if (nextLevelItems !== null && nextLevelItems.length > 0) {
                        for (var j in nextLevelItems) {
                            if (typeof nextLevelItems[j] === "object" && j !== "prevObject") {
                                var secondLevelCurrentText = $(nextLevelItems[j]).find("span.v-navigation__text-wrapper").first().text();
                                if (!exclude_rules.includes(secondLevelCurrentText) && secondLevelCurrentText !== "" && !result_list.includes(secondLevelCurrentText)) {
                                    result_list.push(secondLevelCurrentText);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    return result_list.join("||||");
}

//function GetSourceList(i){$=i;var e=[],t=["OUTLET","New Items","Shop by Brand"],n=$(".v-navigation.v-navigation--mega li.v-navigation__item--level0");if(n.length>0)for(var a in n)if("object"==typeof n[a]&&"prevObject"!==a){var v=n[a],r=$(v).find("a.v-navigation__link--level0 span.v-navigation__text-wrapper");if(r.length>0){var l=$(r.first()).text();t.includes(l)||e.push($(r.first()).text())}var o=$(v).find("ul.v-navigation__list--level0");if(null!==o&&o.length>0){var f=$(o[0]).find("li.v-navigation__item--level1");if(null!==f&&f.length>0)for(var s in f)if("object"==typeof f[s]&&"prevObject"!==s){var g=$(f[s]).find("span.v-navigation__text-wrapper").first().text();t.includes(g)||""===g||e.includes(g)||e.push(g)}}}return e.join("||||")}return GetSourceList(jQuery);
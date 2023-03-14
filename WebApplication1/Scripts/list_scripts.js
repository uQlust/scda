function setupFilter(sel) {
    $.post('@Url.Action("SetupSelection","List")', { m: sel }).done(function (data, textStatus, jqXHR) {
        var txt = '/Filter/Index';
        if (jqXHR.responseText.length > 0)
            window.location.href = txt;
    });

}
function removeFilter(val) {
    var ss = $.post('@Url.Action("RemoveSelection","List")', { m: val })
    window.location.reload();
}
function ClearFiles() {
    var ss = $.post('@Url.Action("ClearFiles","List")')
    window.location.reload();
}
function ClearSuperGenes() {
    var ss = $.post('@Url.Action("ClearSuperGenes","List")')
    window.location.reload();
}

function addFilter(val) {
    $.post('@Url.Action("AddSelection","List")', { m: val }).done(function (data, textStatus, jqXHR) {
        var txt = '/Filter/Index';
        if (jqXHR.responseText.length > 0)
            window.location.href = txt;
    });
    return;
}
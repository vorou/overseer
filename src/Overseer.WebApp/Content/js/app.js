$(function() {
    updateGrid();

    $('form.tender-search').on('submit', function (event) {
        event.preventDefault();
        var q = $(this).find('input.query').val();
        updateGrid(q);
    });
});

var updateGrid = function (q) {
    var uri = "tenders";
    if (q) {
        uri += "?q=" + q;
    }
    $.getJSON(uri, function (data) {
        var tenders = data.tenders;
        var tableRows = [];
        tenders.forEach(function (tender) {
            tableRows.push('<tr><td class="col-sm-1 price">' + tender.price + ' <i class="fa fa-rub"></i></td><td class="col-sm-4"><a href="' + tender.url + '" target="_blank">' + tender.name + ' <i class="fa fa-external-link"></i></a></td><td class="col-sm-1 region">' + tender.regionName + '</td></tr>');
        });
        $('.tender-grid').html(tableRows.join(''));
    });
}
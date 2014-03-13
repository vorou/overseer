$(function() {
    $.getJSON("tenders", function (data) {
        var tenders = data.tenders;
        var tableRows = [];
        tenders.forEach(function (tender) {
            tableRows.push('<tr><td class="col-sm-1 price">' + tender.price + ' <i class="fa fa-rub"></i></td><td class="col-sm-4"><a href="' + tender.url + '" target="_blank">' + tender.name + ' <i class="fa fa-external-link"></i></a></td><td class="col-sm-1 region">' + tender.regionName + '</td></tr>');
        });
        $(tableRows.join('')).appendTo('.tender-grid');
    });
});
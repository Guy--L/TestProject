var parentNodeId = 0;
var site = 'http://localhost:55749';

function cd(nodeid) {
    $('#status').html('Loading directory for ');
    $.getJSON(site + '/browse/' + nodeid, listing);
    $('#status').html('Directory for ');
}

function deep(path)
{
    $('#status').html('Loading directory for ');
    $.getJSON(site + '/deep/' + encodeURI(path), listing);
    $('#status').html('Directory for ');
}

function download(nodeid) {
    window.location = site + '/browse/' + nodeid;
}

function del(nodeid) {
    $.ajax({
        url: site + '/browse/' + nodeid,
        method: 'DELETE',
        contentType: 'application/json',
        success: listing,
        error: function (request, msg, error) {
            // handle failure
        }
    });
}

function upload(nodeid) {
    var form = $('#uploadform');
    var formdata = false;
    if (window.FormData) {
        formdata = new FormData(form[0]);
    }

    $.ajax({
        url: site + '/browse/' + nodeid,
        data: formdata ? formdata : form.serialize(),
        cache: false,
        contentType: false,
        processData: false,
        type: 'POST',
        success: listing,
        error: function (request, msg, error) {
            // handle failure
        }
    });
}

function listing(data) {
    var html = '';
    var current = '';
    var parentid = 0;

    $.each(data, function (idx, obj) {
        if (idx == 0) {
            if (obj.subpath == "\\\\") {        // no uplink at the root
                current = "\\";
                return true;
            }
            current = obj.subpath;
            obj.filename = '..';                // provide uplink
        }
        else {
            parentid = obj.parentid;            // stow parentid for upload form
            if (!obj.isFile)                    // clearly mark directories
                obj.filename += '\\';
        }

        html += '<tr data-id="' + obj.id + '"><td>';
        html += obj.filename == '..' ? '' : '<img src="/images/Erase.png" class="delete" />';
        html += '</td><td class="number" align="right">';

        if (obj.isFile) html += obj.length + '</td><td class="navigate download">';
        else html += obj.count + '</td><td class="navigate"><strong>';

        html += obj.filename;
        if (!obj.isFile) html += '</strong>';

        html += '</td></tr>';
    });
    $('#current').html(current);
    var state = site + '/at' + current.substr(1).replace(/\\/g, "/");
    console.log('pushing ' + state);
    history.pushState(parentid, "", state);
    parentNodeId = parentid;
    $('tbody.list').html(html);

    // reattach events on dynamic content
    $('.navigate').on('click', function () {
        var id = $(this).closest('tr').data('id');
        if ($(this).hasClass('download'))
            download(id);
        else
            cd(id);
    });
    $('img').on('click', function () {
        var id = $(this).closest('tr').data('id');
        if ($(this).hasClass('delete'))
            del(id);
    });
}

$(function () {
    $('#status').html('Loading directory for ');
    $('.main-content .page').hide();

    if (indeep.length == 0)
        cd('');
    else
        deep(indeep);

    $('#current').html('\\');
    $('#action').html('Upload File');
    $('.listing').show();
    window.onpopstate = function (event) {
        var id = 0;
        if (event.state) {
            id = event.state;
        }
        cd(id);
    }
    $('#action').on('click', function () {
        $('.main-content .page').hide();
        if ($(this).hasClass('uploading')) {
            $('#status').html('Loading directory for ');
            cd(parentNodeId);
            $('#status').html('Directory for ');
            $('#action').html('Upload File');
            $(this).removeClass('uploading');
            $('.listing').show();
            return;
        }
        $('#status').html('Upload to ');
        $('#action').html('Cancel Upload');
        $(this).addClass('uploading');
        $('.upload').show();
        console.log('pushing ' + site + '/at' + $('#current').val().replace(/\\/g, "/") + '#upload');
        history.pushState(parentNodeId, "", site + '/at' + $('#current').val().replace(/\\/g, "/")+'#upload');
    });
    $('#upbutton').on('click', function (e) {
        e.preventDefault();
        upload(parentNodeId);
        $('#action').click();
    });
});


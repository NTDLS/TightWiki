﻿$(document).ready(function () {
    $('a[href="#deleteLink"]').click(function (e) {
        var pageNavigation = window.location.pathname.substring(1).split('/')[0];
        var fileName = e.target.id;
        if (confirm("Are you sure you want to delete '" + fileName + "'?")) {
            // do something
            $.ajax({
                type: 'POST',
                url: '/File/Delete/' + pageNavigation + '/' + fileName,
                success: function (result) {
                    $("#uploadedFiles").load("/File/PageAttachments/" + pageNavigation);
                }
            });
        }
    });
});

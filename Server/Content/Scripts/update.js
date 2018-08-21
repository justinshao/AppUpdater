function Init(systemId) {
    /*上传UI工具方法*/
    $.danidemo = $.extend({}, {

        addLog: function (id, status, str) {
            var d = new Date();
            var li = $('<li />', { 'class': 'demo-' + status });

            var message = '[' + d.getHours() + ':' + d.getMinutes() + ':' + d.getSeconds() + ']&nbsp;&nbsp;';

            message += str;

            li.html(message);

            $(id).prepend(li);
        },

        addFile: function (id, i, file) {
            var template = '<div id="demo-file' + i + '">' +
                               '<span class="demo-file-id">#' + i + '</span> - ' + file.name + ' <span class="demo-file-size">(' + $.danidemo.humanizeSize(file.size) + ')</span> <span class="demo-file-status">等待上传</span>' +
                               '<div class="progress progress-striped active">' +
                                   '<div class="progress-bar" role="progressbar" style="width: 0%;">' +
                                       '0%' +
                                   '</div>' +
                               '</div>' +
                           '</div>';

            var i = $(id).attr('file-counter');
            if (!i) {
                $(id).empty();

                i = 0;
            }

            i++;

            $(id).attr('file-counter', i);

            $(id).prepend(template);
        },

        updateFileStatus: function (i, status, message) {
            $('#demo-file' + i).find('span.demo-file-status').html(message).addClass('demo-file-status-' + status);
        },

        updateFileProgress: function (i, percent) {
            $('#demo-file' + i).find('div.progress-bar').width(percent).text(percent);
            if (percent === '100%')
                $('#demo-file' + i).find('div.progress').removeClass('active');
        },

        humanizeSize: function (size) {
            var i = Math.floor(Math.log(size) / Math.log(1024));
            return (size / Math.pow(1024, i)).toFixed(2) * 1 + ' ' + ['B', 'kB', 'MB', 'GB', 'TB'][i];
        }

    }, $.danidemo);
    /*上传配置*/
    $('#drag-and-drop-zone').dmUploader({
        url: '/api/UploadPkg/' + systemId,
        dataType: 'json',
        extFilter: 'zip',
        //maxFiles: 1,
        onInit: function () {
            $.danidemo.addLog('#demo-debug', 'default', '完成初始化');
        },
        onFallbackMode: function (message) {
            $.danidemo.addLog('#demo-debug', 'error', message);
        },
        onNewFile: function (id, file) {
            $.danidemo.addFile('#demo-files', id, file);
        },
        onBeforeUpload: function (id) {
            $.danidemo.addLog('#demo-debug', 'default', '开始上传文件 #' + id);

            $.danidemo.updateFileStatus(id, 'default', '正在上传...');
        },
        onComplete: function () {
            //$.danidemo.addLog('#demo-debug', 'success', '所有上传任务已完成');
        },
        onUploadProgress: function (id, percent) {
            var percentStr = percent + '%';

            $.danidemo.updateFileProgress(id, percentStr);

            if(percent == 100)
                $.danidemo.updateFileStatus(id, 'default', '服务器正在处理，请稍等...');
        },
        onUploadSuccess: function (id, data) {

            if (data.Ok) {
                window.location.reload();
            } else {
                $.danidemo.addLog('#demo-debug', 'error', '文件 #' + id + ' 上传失败：' + data.Message);
                $.danidemo.updateFileStatus(id, 'error', '上传失败：' + data.Message);
            }
        },
        onUploadError: function (id, message) {
            $.danidemo.updateFileStatus(id, 'error', message);

            $.danidemo.addLog('#demo-debug', 'error', '文件 #' + id + '上传失败: ' + message);
        },
        onFileTypeError: function (file) {
            $.danidemo.addLog('#demo-debug', 'error', '文件 \'' + file.name + '\' 格式不正确');
        },
        onFileSizeError: function (file) {
            $.danidemo.addLog('#demo-debug', 'error', '文件 \'' + file.name + '\' 大小超出限制');
        },
        onFileExtError: function (file) {
            $.danidemo.addLog('#demo-debug', 'error', '文件 \'' + file.name + '\' 格式不正确');
        },
        onFilesMaxError: function (file) {
            $.danidemo.addLog('#demo-debug', 'error', '最多允许上传1个文件');
        },
    });

    /*安装更新包*/
    $('#packages').on('click', '.btn-update', function () {
        $(this).text("正在安装...");
        var pkgId = $(this).data('pkgId');
        var url = '/api/InstallPkg/' + systemId + '?pkgId=' + pkgId;

        pkgOption(url);
    });
    /*还原更新包*/
    $('#packages').on('click', '.btn-restore', function () {
        $(this).text("正在还原...");
        var pkgId = $(this).data('pkgId');
        var url = '/api/RestorePkg/' + systemId + '?pkgId=' + pkgId;

        pkgOption(url);
    });
    /*删除更新包*/
    $('#packages').on('click', '.btn-delete', function () {
        $(this).text("正在删除...");
        var pkgId = $(this).data('pkgId');
        var url = '/api/DeletePkg/' + systemId + '?pkgId=' + pkgId;

        pkgOption(url);
    });
    $('#updRunInfo').on('click', function () {
        $(this).text("正在更新运行信息...");
        var url = '/api/UpdateRunInfo/' + systemId;

        pkgOption(url);
    });

    function pkgOption(url) {
        $.ajax({
            url: url,
            type: 'POST',
            cache: false,
            dataType: 'json',
            success: function (ret) {
                if (ret.Ok) {
                    window.location = '/System/' + systemId;
                } else {
                    alert(ret.Message);
                    window.location = '/System/' + systemId;
                }
            },
            error: function () {
                alert('出错了.');
                window.location = '/System/' + systemId;
            }
        });
    }
}


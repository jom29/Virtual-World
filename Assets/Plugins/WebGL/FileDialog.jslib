mergeInto(LibraryManager.library, {
    DownloadFile: function (filenamePtr, dataPtr) {
        var filename = UTF8ToString(filenamePtr);
        var jsonStr = UTF8ToString(dataPtr);
        var blob = new Blob([jsonStr], { type: 'application/json' });

        var link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    },

    UploadFile: function (gameObjectNamePtr, callbackNamePtr) {
        var gameObjectName = UTF8ToString(gameObjectNamePtr);
        var callbackName = UTF8ToString(callbackNamePtr);

        var input = document.createElement('input');
        input.type = 'file';
        input.accept = '.json';

        input.onchange = function (event) {
            var file = event.target.files[0];
            if (!file) return;

            var reader = new FileReader();
            reader.onload = function (e) {
                var json = e.target.result;
                // Call Unity method with loaded JSON string
                SendMessage(gameObjectName, callbackName, json);
            };
            reader.readAsText(file);
        };

        input.click();
    }
});

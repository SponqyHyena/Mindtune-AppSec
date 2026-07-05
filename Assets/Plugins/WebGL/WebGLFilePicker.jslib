mergeInto(LibraryManager.library, {

    WebGLOpenFilePicker: function(goPtr, cbPtr, maxSizeBytes) {
        var go      = UTF8ToString(goPtr);
        var cb      = UTF8ToString(cbPtr);
        var maxSize = maxSizeBytes;

        // window — единственное надёжное хранилище между async-коллбэками и DllImport-функциями.
        // Переменные из mergeInto-объекта не гарантированно видны в замыканиях FileReader.
        window.__unityAvatar = { data: null, error: null };

        var input = document.createElement('input');
        input.type   = 'file';
        input.accept = '.png,.jpg,.jpeg,image/png,image/jpeg';
        input.style.display = 'none';
        document.body.appendChild(input);

        var picked = false;

        input.addEventListener('change', function(e) {
            picked = true;
            if (input.parentNode) document.body.removeChild(input);

            var file = e.target.files[0];
            if (!file) {
                console.log('[AvatarManager] No file selected');
                window.__unityAvatar.error = 'CANCELLED';
                SendMessage(go, 'OnWebGLPickerCancelled', '');
                return;
            }

            console.log('[AvatarManager] File selected:', file.name,
                        'size:', file.size, 'type:', file.type);

            if (file.size > maxSize) {
                console.warn('[AvatarManager] File too large:', file.size, '> maxSize:', maxSize);
                window.__unityAvatar.error = 'TOO_LARGE:' + file.size;
                SendMessage(go, 'OnWebGLPickerError', '');
                return;
            }

            var mime = (file.type || '').toLowerCase();
            if (mime !== 'image/png' && mime !== 'image/jpeg') {
                console.warn('[AvatarManager] Invalid MIME type:', file.type);
                window.__unityAvatar.error = 'INVALID_TYPE:' + file.type;
                SendMessage(go, 'OnWebGLPickerError', '');
                return;
            }

            console.log('[AvatarManager] Starting FileReader...');
            var reader = new FileReader();

            reader.onload = function(ev) {
                console.log('[AvatarManager] FileReader.onload fired, result length:',
                            ev.target.result ? ev.target.result.length : 'null');

                var dataUrl = ev.target.result;
                var comma   = dataUrl ? dataUrl.indexOf(',') : -1;

                if (comma < 0) {
                    console.error('[AvatarManager] Bad data URL, no comma found');
                    window.__unityAvatar.error = 'READ_ERROR:no_comma';
                    SendMessage(go, 'OnWebGLPickerError', '');
                    return;
                }

                window.__unityAvatar.data = dataUrl.substring(comma + 1);
                console.log('[AvatarManager] base64 stored, length:',
                            window.__unityAvatar.data.length,
                            '| Calling SendMessage →', go, cb);

                SendMessage(go, cb, '');
                console.log('[AvatarManager] SendMessage done');
            };

            reader.onerror = function(err) {
                console.error('[AvatarManager] FileReader error:', err);
                window.__unityAvatar.error = 'READ_ERROR:file_reader';
                SendMessage(go, 'OnWebGLPickerError', '');
            };

            reader.readAsDataURL(file);
        });

        // Хак для определения отмены: window получает фокус после закрытия диалога
        var focusHandler = function() {
            window.removeEventListener('focus', focusHandler);
            setTimeout(function() {
                if (picked) return; // change уже сработал — не отменяем
                picked = true;
                if (input.parentNode) document.body.removeChild(input);
                console.log('[AvatarManager] Picker cancelled (focus-hack)');
                window.__unityAvatar.error = 'CANCELLED';
                SendMessage(go, 'OnWebGLPickerCancelled', '');
            }, 800); // 800ms — запас для медленного FileReader на старых машинах
        };
        window.addEventListener('focus', focusHandler);

        input.click();
        console.log('[AvatarManager] File picker opened');
    },

    WebGLFetchAvatarData: function() {
        if (!window.__unityAvatar || !window.__unityAvatar.data) {
            console.warn('[AvatarManager] WebGLFetchAvatarData: store is empty');
            return 0;
        }
        var str = window.__unityAvatar.data;
        window.__unityAvatar.data = null;
        console.log('[AvatarManager] WebGLFetchAvatarData: writing', str.length, 'chars to heap');
        var len = lengthBytesUTF8(str) + 1;
        var buf = _malloc(len);
        stringToUTF8(str, buf, len);
        return buf;
    },

    WebGLFetchAvatarError: function() {
        if (!window.__unityAvatar || !window.__unityAvatar.error) return 0;
        var str = window.__unityAvatar.error;
        window.__unityAvatar.error = null;
        var len = lengthBytesUTF8(str) + 1;
        var buf = _malloc(len);
        stringToUTF8(str, buf, len);
        return buf;
    }
});

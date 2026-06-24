// ─────────────────────────────────────────────────────────────
// [추가] 곡 업로드 Swagger 폼 자동완성
//  업로드용 File 칸에서 mp3 등을 선택하면, 브라우저에서 ID3 태그와
//  재생시간을 읽어 Title·Artist·Album·Genre·Year·Duration·Lyrics 칸을
//  자동으로 채워준다. (Swagger UI 는 React 제어 input 이라 네이티브 setter 로 주입)
// ─────────────────────────────────────────────────────────────
(function () {
    "use strict";

    // jsmediatags(ID3 태그 리더) 동적 로드
    function loadTagLib(cb) {
        if (window.jsmediatags) { cb(); return; }
        var s = document.createElement("script");
        s.src = "https://cdn.jsdelivr.net/npm/jsmediatags@3.9.7/dist/jsmediatags.min.js";
        s.onload = cb;
        s.onerror = function () { console.warn("[autofill] jsmediatags 로드 실패 (인터넷 연결 확인)"); };
        document.head.appendChild(s);
    }

    // React 제어 input/textarea 에 값 주입 (네이티브 setter + 이벤트)
    function setValue(el, value) {
        if (!el || value === undefined || value === null) return;
        var str = String(value).trim();
        if (str === "") return;
        var proto = el.tagName === "TEXTAREA"
            ? window.HTMLTextAreaElement.prototype
            : window.HTMLInputElement.prototype;
        var setter = Object.getOwnPropertyDescriptor(proto, "value").set;
        setter.call(el, str);
        el.dispatchEvent(new Event("input", { bubbles: true }));
        el.dispatchEvent(new Event("change", { bubbles: true }));
    }

    // 행에서 라벨명 추출 (Swagger UI 버전에 따라 클래스가 달라 폴백 여러 개)
    function rowName(row) {
        var n = row.querySelector(".parameter__name")
            || row.querySelector(".parameters-col_name")
            || row.querySelector("td");
        if (!n) return "";
        // 타입 표기(string 등)는 별도 요소라 textContent 첫 단어만 사용
        return n.textContent.replace(/\*/g, "").trim().split(/\s+/)[0].toLowerCase();
    }

    // 같은 업로드 블록 안에서 라벨명으로 입력칸 찾기
    function findInput(scope, label) {
        var rows = scope.querySelectorAll("tr");
        var want = label.toLowerCase();
        for (var i = 0; i < rows.length; i++) {
            if (rowName(rows[i]) === want) {
                return rows[i].querySelector("input:not([type=file]), textarea");
            }
        }
        return null;
    }

    function isAudio(name) {
        return /\.(mp3|flac|m4a|aac|wav|ogg|wma)$/i.test(name || "");
    }

    function fillFromFile(fileInput, file) {
        var scope = fileInput.closest(".opblock") || document;

        // 1) ID3 태그
        window.jsmediatags.read(file, {
            onSuccess: function (result) {
                var t = (result && result.tags) || {};
                setValue(findInput(scope, "Title"), t.title);
                setValue(findInput(scope, "Artist"), t.artist);
                setValue(findInput(scope, "Album"), t.album);
                setValue(findInput(scope, "Genre"), t.genre);
                setValue(findInput(scope, "Year"), t.year);
                if (t.lyrics && t.lyrics.lyrics) {
                    setValue(findInput(scope, "Lyrics"), t.lyrics.lyrics);
                }
                // 제목이 비어 있으면 파일명으로 폴백
                var titleEl = findInput(scope, "Title");
                if (titleEl && !titleEl.value) {
                    setValue(titleEl, file.name.replace(/\.[^.]+$/, ""));
                }
            },
            onError: function () {
                // 태그가 없으면 파일명이라도 제목에 채움
                setValue(findInput(scope, "Title"), file.name.replace(/\.[^.]+$/, ""));
            }
        });

        // 2) 재생시간(duration) — 오디오 메타데이터에서
        try {
            var url = URL.createObjectURL(file);
            var audio = new Audio();
            audio.preload = "metadata";
            audio.onloadedmetadata = function () {
                if (isFinite(audio.duration) && audio.duration > 0) {
                    setValue(findInput(scope, "Duration"), Math.round(audio.duration));
                }
                URL.revokeObjectURL(url);
            };
            audio.src = url;
        } catch (e) { /* 무시 */ }
    }

    // 파일 선택 감지 (동적으로 그려지는 폼이라 document 위임)
    document.addEventListener("change", function (e) {
        var el = e.target;
        if (el && el.type === "file" && el.files && el.files[0] && isAudio(el.files[0].name)) {
            var file = el.files[0];
            loadTagLib(function () { fillFromFile(el, file); });
        }
    }, true);

    console.log("[autofill] 곡 업로드 자동완성 스크립트 로드됨");
})();

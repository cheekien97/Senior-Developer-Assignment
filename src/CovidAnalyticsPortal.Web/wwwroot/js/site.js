// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ---------------------------------------------------------------------------
// Enhanced tables: per-column search, column sorting and pagination.
// Opt-in via `data-enhance` on a <table>. Optional wiring via data attributes:
//   data-page-size="#selector"  -> <select> controlling rows per page
//   data-pager="#selector"      -> <ul> that receives pagination buttons
//   data-page-info="#selector"  -> element that receives "Showing x-y of z"
// A filter input is added under every header cell; rows must match every
// active column filter (AND). Add `data-no-filter` to a <th> to skip it.
// Sortable columns: add `data-sort-type="text|number|date"` to a <th>.
// For reliable sorting, cells may carry `data-sort-value` (raw ISO/number).
// ---------------------------------------------------------------------------
(function () {
    "use strict";

    function pick(attr) {
        return attr ? document.querySelector(attr) : null;
    }

    function cellValue(cell, type) {
        var raw = cell.dataset.sortValue !== undefined ? cell.dataset.sortValue : cell.textContent.trim();
        if (type === "number") {
            var n = parseFloat(raw.replace(/[^0-9.\-]/g, ""));
            return isNaN(n) ? Number.NEGATIVE_INFINITY : n;
        }
        if (type === "date") {
            var t = Date.parse(raw);
            return isNaN(t) ? 0 : t;
        }
        return raw.toLowerCase();
    }

    function enhance(table) {
        if (!table.tHead || !table.tBodies.length) return;

        var headers = Array.prototype.slice.call(table.tHead.rows[0].cells);
        var allRows = Array.prototype.slice.call(table.tBodies[0].rows);

        var pageSizeSel = pick(table.dataset.pageSize);
        var pager = pick(table.dataset.pager);
        var info = pick(table.dataset.pageInfo);

        var filtered = allRows.slice();
        var colFilters = headers.map(function () { return ""; });
        var pageSize = pageSizeSel ? parseInt(pageSizeSel.value, 10) : Number.MAX_SAFE_INTEGER;
        var current = 1;
        var sortCol = -1;
        var sortDir = 1;

        function applySearch() {
            var active = colFilters.some(function (q) { return q; });
            if (!active) {
                filtered = allRows.slice();
                return;
            }
            filtered = allRows.filter(function (r) {
                return colFilters.every(function (q, idx) {
                    if (!q) return true;
                    var cell = r.cells[idx];
                    return cell ? cell.textContent.toLowerCase().indexOf(q) !== -1 : false;
                });
            });
        }

        function applySort() {
            if (sortCol < 0) return;
            var type = headers[sortCol].dataset.sortType || "text";
            filtered.sort(function (a, b) {
                var av = cellValue(a.cells[sortCol], type);
                var bv = cellValue(b.cells[sortCol], type);
                if (av < bv) return -1 * sortDir;
                if (av > bv) return 1 * sortDir;
                return 0;
            });
        }

        function totalPages() {
            return Math.max(1, Math.ceil(filtered.length / pageSize));
        }

        function pageItem(label, page, opts) {
            opts = opts || {};
            var li = document.createElement("li");
            li.className = "page-item" + (opts.active ? " active" : "") + (opts.disabled ? " disabled" : "");
            var a = document.createElement("a");
            a.className = "page-link";
            a.href = "#";
            a.innerHTML = label;
            a.addEventListener("click", function (e) {
                e.preventDefault();
                if (opts.disabled || opts.active || !page) return;
                current = page;
                render();
            });
            li.appendChild(a);
            return li;
        }

        function buildPager(pages) {
            if (!pager) return;
            pager.innerHTML = "";
            if (pages <= 1) return;
            pager.appendChild(pageItem("&laquo;", current - 1, { disabled: current === 1 }));

            var win = 2;
            var nums = {};
            nums[1] = true;
            nums[pages] = true;
            for (var p = current - win; p <= current + win; p++) {
                if (p >= 1 && p <= pages) nums[p] = true;
            }
            var sorted = Object.keys(nums).map(Number).sort(function (a, b) { return a - b; });
            var prev = 0;
            sorted.forEach(function (p) {
                if (p - prev > 1) {
                    var gap = document.createElement("li");
                    gap.className = "page-item disabled";
                    gap.innerHTML = '<span class="page-link">…</span>';
                    pager.appendChild(gap);
                }
                pager.appendChild(pageItem(String(p), p, { active: p === current }));
                prev = p;
            });

            pager.appendChild(pageItem("&raquo;", current + 1, { disabled: current === pages }));
        }

        function render() {
            var tbody = table.tBodies[0];
            // Re-attach rows in the current filtered/sorted order.
            filtered.forEach(function (r) { tbody.appendChild(r); });
            allRows.forEach(function (r) {
                if (filtered.indexOf(r) === -1) r.style.display = "none";
            });

            var pages = totalPages();
            current = Math.min(current, pages);
            var start = (current - 1) * pageSize;
            var end = start + pageSize;

            filtered.forEach(function (r, i) {
                r.style.display = (i >= start && i < end) ? "" : "none";
            });

            if (info) {
                var shown = Math.min(end, filtered.length);
                info.textContent = "Showing " + (filtered.length ? start + 1 : 0) + "–" + shown + " of " + filtered.length;
            }
            buildPager(pages);
        }

        function refresh(resetPage) {
            applySearch();
            applySort();
            if (resetPage) current = 1;
            render();
        }

        // Wire sortable headers.
        headers.forEach(function (th, idx) {
            if (!th.dataset.sortType) return;
            th.classList.add("th-sortable");
            var indicator = document.createElement("span");
            indicator.className = "sort-indicator";
            th.appendChild(indicator);
            th.addEventListener("click", function () {
                if (sortCol === idx) {
                    if (sortDir === 1) {
                        // ascending -> descending
                        sortDir = -1;
                    } else {
                        // descending -> clear sorting (restore original order)
                        sortCol = -1;
                        sortDir = 1;
                    }
                } else {
                    sortCol = idx;
                    sortDir = 1;
                }
                headers.forEach(function (h) {
                    h.classList.remove("sorted-asc", "sorted-desc");
                });
                if (sortCol === idx) {
                    th.classList.add(sortDir === 1 ? "sorted-asc" : "sorted-desc");
                }
                refresh(true);
            });
        });

        // Add a search icon + pop-out filter input to each searchable header.
        var openPop = null;

        function closePop() {
            if (openPop) {
                openPop.classList.remove("open");
                openPop = null;
            }
        }

        function positionPop(btn, pop) {
            // Fixed positioning so the pop-out is never clipped by the table's
            // horizontal scroll container; clamp it inside the viewport.
            var rect = btn.getBoundingClientRect();
            var width = pop.offsetWidth || 192;
            var left = rect.right - width;
            var margin = 8;
            if (left < margin) left = margin;
            if (left + width > window.innerWidth - margin) {
                left = window.innerWidth - margin - width;
            }
            pop.style.top = (rect.bottom + 4) + "px";
            pop.style.left = left + "px";
        }

        document.addEventListener("click", closePop);
        window.addEventListener("resize", closePop);
        // Close the pop-out on any scroll (table body or page) to avoid drift.
        document.addEventListener("scroll", closePop, true);

        headers.forEach(function (th, idx) {
            if (th.dataset.noFilter !== undefined) return;

            var label = (th.textContent || "").trim() || "column";
            th.classList.add("th-has-search");

            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "th-search-btn";
            btn.setAttribute("aria-label", "Search " + label);
            btn.innerHTML = '<i class="bi bi-search"></i>';

            var pop = document.createElement("div");
            pop.className = "th-search-pop";
            var input = document.createElement("input");
            input.type = "search";
            input.className = "form-control form-control-sm";
            input.placeholder = "Search " + label;
            input.setAttribute("aria-label", "Search " + label);
            pop.appendChild(input);

            input.addEventListener("input", function () {
                colFilters[idx] = input.value.trim().toLowerCase();
                btn.classList.toggle("filtered", colFilters[idx] !== "");
                refresh(true);
            });
            input.addEventListener("keydown", function (e) {
                if (e.key === "Enter" || e.key === "Escape") {
                    e.preventDefault();
                    closePop();
                    btn.focus();
                }
            });
            // Keep clicks inside the pop-out from sorting or closing.
            pop.addEventListener("click", function (e) { e.stopPropagation(); });
            input.addEventListener("click", function (e) { e.stopPropagation(); });

            btn.addEventListener("click", function (e) {
                e.stopPropagation();
                var isOpen = pop.classList.contains("open");
                closePop();
                if (!isOpen) {
                    pop.classList.add("open");
                    positionPop(btn, pop);
                    openPop = pop;
                    input.focus();
                }
            });

            th.appendChild(btn);
            // Attach the pop-out to <body> so it escapes the table's overflow.
            document.body.appendChild(pop);
        });

        if (pageSizeSel) {
            pageSizeSel.addEventListener("change", function () {
                pageSize = parseInt(pageSizeSel.value, 10);
                refresh(true);
            });
        }

        render();
    }

    // -----------------------------------------------------------------------
    // Date-range picker: replaces a pair of From/To date inputs with a single
    // calendar that supports selecting a range. Opt-in via `data-date-range`
    // on a text <input>; the script keeps two hidden inputs (resolved from
    // data-from / data-to selectors, or by name="From"/name="To" within the
    // same form) in sync using ISO yyyy-MM-dd values for model binding.
    // -----------------------------------------------------------------------
    function isoDate(d) {
        return d.getFullYear() + "-" +
            String(d.getMonth() + 1).padStart(2, "0") + "-" +
            String(d.getDate()).padStart(2, "0");
    }

    function initDateRange(input) {
        if (typeof window.flatpickr === "undefined") {
            return;
        }

        var form = input.closest("form");
        var scope = form || document;
        var fromInput = pick(input.dataset.from) ||
            scope.querySelector('input[name="From"]');
        var toInput = pick(input.dataset.to) ||
            scope.querySelector('input[name="To"]');
        if (!fromInput || !toInput) {
            return;
        }

        var defaults = [];
        if (fromInput.value) { defaults.push(fromInput.value); }
        if (toInput.value) { defaults.push(toInput.value); }

        flatpickr(input, {
            mode: "range",
            dateFormat: "Y-m-d",
            altInput: true,
            altFormat: "d M Y",
            maxDate: input.dataset.max || null,
            defaultDate: defaults.length ? defaults : null,
            onChange: function (selectedDates) {
                if (selectedDates.length === 2) {
                    fromInput.value = isoDate(selectedDates[0]);
                    toInput.value = isoDate(selectedDates[1]);
                } else if (selectedDates.length === 1) {
                    fromInput.value = isoDate(selectedDates[0]);
                    toInput.value = isoDate(selectedDates[0]);
                }
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        Array.prototype.slice
            .call(document.querySelectorAll("table[data-enhance]"))
            .forEach(enhance);

        Array.prototype.slice
            .call(document.querySelectorAll("input[data-date-range]"))
            .forEach(initDateRange);

        // Enable Bootstrap tooltips wherever data-bs-toggle="tooltip" is used.
        if (window.bootstrap && bootstrap.Tooltip) {
            Array.prototype.slice
                .call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
                .forEach(function (el) { new bootstrap.Tooltip(el); });
        }
    });
})();

// Rectangle primitive — lightweight-charts has no built-in box annotation, so zones
// (dominant level, fair value gap, order block) are drawn through a series primitive.
function createRectanglePrimitive() {
    class RectangleRenderer {
        constructor(source) { this._source = source; }

        draw(target) {
            var source = this._source;
            if (!source._chart || !source._series || source._rects.length === 0)
                return;

            // Everything below is wrapped: an exception thrown from a primitive aborts the whole
            // render pass, which wiped the chart to a blank background instead of just dropping
            // the annotation.
            try {
                var timeScale = source._chart.timeScale();

                // Visible window, needed to place zones that start before or end after it —
                // timeToCoordinate returns null outside the range and cannot position those.
                var visible = null;
                try { visible = timeScale.getVisibleRange(); } catch (e) { }

                target.useBitmapCoordinateSpace(function (scope) {
                    var ctx = scope.context;
                    var ratio = scope.horizontalPixelRatio;
                    var vRatio = scope.verticalPixelRatio;
                    var widthPx = scope.mediaSize.width;

                    // Coordinate for a zone edge, clamped to the chart edges when the time falls
                    // outside the visible window (or is absent, for a zone that is still open).
                    var edgeX = function (time, fallback) {
                        if (time === null || time === undefined)
                            return fallback;

                        var x = null;
                        try { x = timeScale.timeToCoordinate(time); } catch (e) { x = null; }
                        if (x !== null)
                            return x;

                        if (visible) {
                            if (time < visible.from) return 0;
                            if (time > visible.to) return widthPx;
                        }
                        return fallback;
                    };

                    source._rects.forEach(function (r) {
                        var y1 = source._series.priceToCoordinate(r.price1);
                        var y2 = source._series.priceToCoordinate(r.price2);
                        if (!isFinite(y1) || !isFinite(y2)) return;

                        var x1 = edgeX(r.time1, 0);
                        // A zone that is still active runs to the right edge of the chart
                        var x2 = edgeX(r.time2, widthPx);
                        if (!isFinite(x1) || !isFinite(x2)) return;

                        var left = Math.min(x1, x2) * ratio;
                        var right = Math.max(x1, x2) * ratio;
                        var top = Math.min(y1, y2) * vRatio;
                        var bottom = Math.max(y1, y2) * vRatio;

                        // Nothing to paint, and a zero-width box would swallow the label
                        if (right - left < 1 || bottom - top < 1) return;

                        ctx.fillStyle = r.fill;
                        ctx.fillRect(left, top, right - left, bottom - top);

                        if (r.border) {
                            ctx.strokeStyle = r.border;
                            ctx.lineWidth = 1 * ratio;
                            ctx.strokeRect(left, top, right - left, bottom - top);
                        }

                        if (r.text) {
                            ctx.fillStyle = r.textColor || '#ffffff';
                            ctx.font = Math.round(10 * vRatio) + 'px sans-serif';
                            ctx.fillText(r.text, left + 3 * ratio, top + 12 * vRatio);
                        }
                    });
                });
            }
            catch (e) {
                if (window.console) console.error('zone overlay draw failed', e);
            }
        }
    }

    class RectanglePaneView {
        constructor(source) { this._renderer = new RectangleRenderer(source); }
        renderer() { return this._renderer; }
        zOrder() { return 'bottom'; }
    }

    class RectanglePrimitive {
        constructor(rects) {
            this._rects = rects || [];
            this._paneView = new RectanglePaneView(this);
        }
        attached(param) {
            this._chart = param.chart;
            this._series = param.series;
            this._requestUpdate = param.requestUpdate;
        }
        detached() { }
        paneViews() { return [this._paneView]; }
        updateAllViews() { }
        setRects(rects) {
            this._rects = rects || [];
            if (this._requestUpdate) this._requestUpdate();
        }
    }

    return RectanglePrimitive;
}

// Measure primitive — drag a box over the chart to read the move as a percentage, the way the
// old chart window could. lightweight-charts has no measuring tool of its own.
function createMeasurePrimitive() {
    var UP_FILL = 'rgba(38,166,154,0.18)';
    var UP_LINE = 'rgba(38,166,154,0.9)';
    var DOWN_FILL = 'rgba(239,83,80,0.18)';
    var DOWN_LINE = 'rgba(239,83,80,0.9)';

    class MeasureRenderer {
        constructor(source) { this._source = source; }

        draw(target) {
            var source = this._source;
            var m = source._measure;
            if (!m || !m.active) return;

            try {
                var timeScale = source._chart.timeScale();

                target.useBitmapCoordinateSpace(function (scope) {
                    var ctx = scope.context;
                    var hRatio = scope.horizontalPixelRatio;
                    var vRatio = scope.verticalPixelRatio;

                    var x1 = m.x1, x2 = m.x2;
                    var y1 = source._series.priceToCoordinate(m.price1);
                    var y2 = source._series.priceToCoordinate(m.price2);
                    if (y1 === null || y2 === null) return;

                    var rising = m.price2 >= m.price1;
                    var left = Math.min(x1, x2) * hRatio;
                    var right = Math.max(x1, x2) * hRatio;
                    var top = Math.min(y1, y2) * vRatio;
                    var bottom = Math.max(y1, y2) * vRatio;

                    ctx.fillStyle = rising ? UP_FILL : DOWN_FILL;
                    ctx.fillRect(left, top, right - left, bottom - top);

                    ctx.strokeStyle = rising ? UP_LINE : DOWN_LINE;
                    ctx.lineWidth = 1 * hRatio;
                    ctx.strokeRect(left, top, right - left, bottom - top);

                    // Arrow along the price move, so the direction is readable at a glance
                    var midX = (left + right) / 2;
                    var fromY = (rising ? bottom : top);
                    var toY = (rising ? top : bottom);
                    ctx.beginPath();
                    ctx.moveTo(midX, fromY);
                    ctx.lineTo(midX, toY);
                    ctx.stroke();

                    // Label block
                    var pct = m.price1 === 0 ? 0 : (m.price2 - m.price1) / m.price1 * 100;
                    var lines = [
                        (pct >= 0 ? '+' : '') + pct.toFixed(2) + '%',
                        source._formatPrice(m.price2 - m.price1),
                        m.bars + (m.bars === 1 ? ' candle' : ' candles') + (m.span ? '  ' + m.span : ''),
                        'click or Esc to clear',
                    ];

                    var fontPx = Math.round(11 * vRatio);
                    ctx.font = fontPx + 'px sans-serif';
                    var padding = 6 * hRatio;
                    var lineHeight = fontPx * 1.35;

                    var boxWidth = 0;
                    for (var i = 0; i < lines.length; i++)
                        boxWidth = Math.max(boxWidth, ctx.measureText(lines[i]).width);
                    boxWidth += padding * 2;
                    var boxHeight = lineHeight * lines.length + padding;

                    // Sit the label just past the end of the drag, flipped when it would clip
                    var boxX = (Math.max(x1, x2) + 8) * hRatio;
                    if (boxX + boxWidth > scope.bitmapSize.width)
                        boxX = (Math.min(x1, x2) * hRatio) - boxWidth - 8 * hRatio;
                    if (boxX < 0) boxX = 0;

                    var boxY = toY - boxHeight / 2;
                    if (boxY < 0) boxY = 0;
                    if (boxY + boxHeight > scope.bitmapSize.height)
                        boxY = scope.bitmapSize.height - boxHeight;

                    ctx.fillStyle = rising ? UP_LINE : DOWN_LINE;
                    ctx.fillRect(boxX, boxY, boxWidth, boxHeight);

                    ctx.textBaseline = 'top';
                    for (var j = 0; j < lines.length; j++) {
                        // Last line is the how-to-dismiss hint, kept quieter than the numbers
                        var isHint = j === lines.length - 1;
                        ctx.fillStyle = isHint ? 'rgba(255,255,255,0.75)' : '#ffffff';
                        ctx.font = (isHint ? Math.round(fontPx * 0.82) : fontPx) + 'px sans-serif';
                        ctx.fillText(lines[j], boxX + padding, boxY + padding / 2 + j * lineHeight);
                    }
                });
            }
            catch (e) {
                if (window.console) console.error('measure draw failed', e);
            }
        }
    }

    class MeasurePaneView {
        constructor(source) { this._renderer = new MeasureRenderer(source); }
        renderer() { return this._renderer; }
        zOrder() { return 'top'; }
    }

    class MeasurePrimitive {
        constructor(formatPrice) {
            this._measure = null;
            this._formatPrice = formatPrice;
            this._paneView = new MeasurePaneView(this);
        }
        attached(param) {
            this._chart = param.chart;
            this._series = param.series;
            this._requestUpdate = param.requestUpdate;
        }
        detached() { }
        paneViews() { return [this._paneView]; }
        updateAllViews() { }
        setMeasure(measure) {
            this._measure = measure;
            if (this._requestUpdate) this._requestUpdate();
        }
    }

    return MeasurePrimitive;
}

// Segment primitive — bounded line pieces with an optional caption, the way the Avalonia chart
// draws position levels. lightweight-charts only offers createPriceLine, which always spans the
// whole width; with an entry, several DCA levels, a take profit and a stop that turns into a wall
// of full-width lines with no telling which belongs to which position.
function createSegmentPrimitive() {
    var DASH = {
        0: [],        // solid
        1: [2, 4],    // dotted, for the vertical open markers
        // Position levels: dotted with a wide gap. The dash-dash-dot of the Avalonia original
        // drew far too much attention on this chart, where the lines are thinner and darker.
        // Dots half a pixel longer than the plain dotted pattern above, because at 1 pixel the
        // horizontal levels were hard to pick out against the candles.
        2: [1.5, 6],
    };

    class SegmentRenderer {
        constructor(source) { this._source = source; }

        draw(target) {
            var source = this._source;
            if (!source._chart || !source._series)
                return;
            if (source._segments.length === 0 && source._labels.length === 0 && source._dots.length === 0)
                return;

            try {
                var timeScale = source._chart.timeScale();
                var visible = null;
                try { visible = timeScale.getVisibleRange(); } catch (e) { }

                target.useBitmapCoordinateSpace(function (scope) {
                    var ctx = scope.context;
                    var hRatio = scope.horizontalPixelRatio;
                    var vRatio = scope.verticalPixelRatio;
                    var widthPx = scope.mediaSize.width;

                    // Clamped to the chart edges when the time falls outside the visible window,
                    // so a segment that started before it still shows the part that is in view
                    var edgeX = function (time, fallback) {
                        if (time === null || time === undefined) return fallback;
                        var x = null;
                        try { x = timeScale.timeToCoordinate(time); } catch (e) { x = null; }
                        if (x !== null) return x;
                        if (visible) {
                            if (time < visible.from) return 0;
                            if (time > visible.to) return widthPx;
                        }
                        return fallback;
                    };

                    // try/finally around save/restore: an exception between the two leaves the
                    // canvas with a pushed state, and every later draw then inherits a wrong
                    // transform or clip. That is what turned the whole chart black instead of
                    // just dropping this one overlay.
                    ctx.save();
                    try {
                    source._segments.forEach(function (s) {
                        var y1 = source._series.priceToCoordinate(s.price1);
                        var y2 = source._series.priceToCoordinate(s.price2 !== undefined && s.price2 !== null ? s.price2 : s.price1);
                        if (!isFinite(y1) || !isFinite(y2)) return;

                        var x1 = edgeX(s.time1, 0);
                        var x2 = edgeX(s.time2, widthPx);
                        if (!isFinite(x1) || !isFinite(x2)) return;

                        ctx.strokeStyle = s.color;
                        ctx.lineWidth = (s.width || 2) * hRatio;
                        ctx.setLineDash((DASH[s.dash] || []).map(function (d) { return d * hRatio; }));

                        ctx.beginPath();
                        ctx.moveTo(x1 * hRatio, y1 * vRatio);
                        ctx.lineTo(x2 * hRatio, y2 * vRatio);
                        ctx.stroke();

                        if (s.text) {
                            // Normally at the start of the line, but a vertical marker puts its
                            // caption at a price of its own so it does not end up at the far end
                            // of the line
                            var yText = y1;
                            if (s.textPrice !== undefined && s.textPrice !== null) {
                                var yOwn = source._series.priceToCoordinate(s.textPrice);
                                if (isFinite(yOwn)) yText = yOwn;
                            }

                            ctx.setLineDash([]);
                            ctx.fillStyle = '#ffffff';
                            ctx.font = Math.round(9 * vRatio) + 'px sans-serif';
                            ctx.textBaseline = 'bottom';
                            // Just right of the start of the line, same as the Avalonia annotation
                            ctx.fillText(s.text, (Math.min(x1, x2) + 4) * hRatio, yText * vRatio - 2 * vRatio);
                        }
                    });
                    // A small filled circle where an order actually filled. Deliberately not a
                    // series marker: the shapes lightweight-charts offers there start far bigger
                    // than the candles and sat on the chart as blobs.
                    ctx.setLineDash([]);
                    source._dots.forEach(function (d) {
                        var y = source._series.priceToCoordinate(d.price);
                        if (!isFinite(y)) return;

                        var x = null;
                        try { x = timeScale.timeToCoordinate(d.time); } catch (e) { x = null; }
                        if (!isFinite(x)) return;

                        var radius = (d.radius || 3) * hRatio;
                        ctx.beginPath();
                        ctx.arc(x * hRatio, y * vRatio, radius, 0, 2 * Math.PI);
                        ctx.fillStyle = d.color || '#ffffff';
                        ctx.fill();

                        // Thin dark rim, otherwise a dot on a candle of its own colour vanishes
                        ctx.lineWidth = 1 * hRatio;
                        ctx.strokeStyle = 'rgba(0,0,0,0.65)';
                        ctx.stroke();
                    });

                    // Captions of the band overlays. Sorted by time, and one is skipped when it
                    // would land on top of the previous one — so zooming in reveals more of them
                    // instead of leaving an unreadable pile at every band break.
                    var lastRight = { above: -1e9, below: -1e9 };
                    ctx.font = Math.round(10 * vRatio) + 'px sans-serif';
                    source._labels.forEach(function (l) {
                        var y = source._series.priceToCoordinate(l.price);
                        if (!isFinite(y)) return;

                        var x = null;
                        try { x = timeScale.timeToCoordinate(l.time); } catch (e) { x = null; }
                        if (!isFinite(x)) return;

                        var w = ctx.measureText(l.text).width;
                        var left = x * hRatio - w / 2;
                        var lane = l.above ? 'above' : 'below';
                        if (left < lastRight[lane]) return;
                        lastRight[lane] = left + w + 6 * hRatio;

                        ctx.fillStyle = l.color || '#ffffff';
                        ctx.textBaseline = l.above ? 'bottom' : 'top';
                        ctx.fillText(l.text, left, y * vRatio + (l.above ? -8 : 8) * vRatio);
                    });
                    }
                    finally {
                        ctx.restore();
                    }
                });
            }
            catch (e) {
                if (window.console) console.error('segment overlay draw failed', e);
            }
        }
    }

    class SegmentPaneView {
        constructor(source) { this._renderer = new SegmentRenderer(source); }
        renderer() { return this._renderer; }
        zOrder() { return 'top'; }
    }

    class SegmentPrimitive {
        constructor(segments) {
            this._segments = segments || [];
            this._labels = [];
            this._dots = [];
            this._paneView = new SegmentPaneView(this);
        }
        attached(param) {
            this._chart = param.chart;
            this._series = param.series;
            this._requestUpdate = param.requestUpdate;
        }
        detached() { }
        paneViews() { return [this._paneView]; }
        updateAllViews() { }
        setSegments(segments) {
            this._segments = segments || [];
            if (this._requestUpdate) this._requestUpdate();
        }
        setLabels(labels) {
            // Sorted so the collision test below only has to look at the previous one
            this._labels = (labels || []).slice().sort(function (a, b) { return a.time - b.time; });
            if (this._requestUpdate) this._requestUpdate();
        }
        setDots(dots) {
            this._dots = dots || [];
            if (this._requestUpdate) this._requestUpdate();
        }
    }

    return SegmentPrimitive;
}

window.ChartWidget = {
    _charts: {},
    _isDark: true,
    _loaded: false,
    _syncing: false,
    _zonePrimitive: null,
    _segmentPrimitive: null,
    _RectanglePrimitive: null,
    _SegmentPrimitive: null,
    _MeasurePrimitive: null,
    _measurePrimitive: null,
    _measureHandlers: null,
    _verticalHandlers: null,
    _interactionSuspended: false,
    _pendingFit: true,
    _lastCandles: null,

    ensureLibrary: function () {
        return new Promise(function (resolve, reject) {
            if (window.LightweightCharts) { resolve(); return; }
            var script = document.createElement('script');
            script.src = 'https://unpkg.com/lightweight-charts@4.2.0/dist/lightweight-charts.standalone.production.js';
            script.onload = function () { resolve(); };
            script.onerror = function () { reject('Failed to load lightweight-charts'); };
            document.head.appendChild(script);
        });
    },

    init: async function (theme) {
        await this.ensureLibrary();
        // Anything that is not explicitly light counts as dark, and case does not matter. The
        // caller used to pass "Dark" straight from the settings, which failed a === 'dark' test.
        this._isDark = String(theme || '').toLowerCase() !== 'light';
        this._RectanglePrimitive = createRectanglePrimitive();
        this._SegmentPrimitive = createSegmentPrimitive();
        this._MeasurePrimitive = createMeasurePrimitive();
        this.dispose();
        this._createMainChart();
        this._attachVerticalControl();
        this._attachMeasureTool();
        this._loaded = true;
    },

    clearMeasure: function () {
        if (this._measurePrimitive)
            this._measurePrimitive.setMeasure(null);
    },

    _formatMeasurePrice: function (value) {
        var abs = Math.abs(value);
        var decimals = abs >= 100 ? 2 : abs >= 1 ? 4 : abs >= 0.01 ? 6 : 8;
        return (value >= 0 ? '+' : '') + value.toFixed(decimals);
    },

    _formatSpan: function (seconds) {
        seconds = Math.abs(seconds);
        if (seconds < 3600)
            return Math.round(seconds / 60) + 'm';

        var days = Math.floor(seconds / 86400);
        var hours = Math.round((seconds % 86400) / 3600);
        if (days > 0)
            return days + 'd' + (hours > 0 ? ' ' + hours + 'h' : '');
        return Math.round(seconds / 3600) + 'h';
    },

    // Vertical zoom and pan. lightweight-charts drags and wheel-zooms the TIME axis only; the price
    // axis can just be dragged on the axis itself, and IPriceScaleApi exposes no way to set a price
    // range. scaleMargins is the one lever there is: it says how much of the pane height stays
    // empty above and below the data, so shrinking both margins magnifies the candles and shifting
    // them against each other moves the data up or down.
    _priceMargins: { top: 0.2, bottom: 0.1 },

    _applyPriceMargins: function () {
        var m = this._priceMargins;

        // Never let the data get squeezed into a sliver: at least 40% of the pane height stays
        // available for it. Beyond that the candles collapse onto one line and the price axis has
        // no room left to place its labels, which is what made the axis look like it disappeared.
        m.top = Math.min(Math.max(m.top, 0), 0.4);
        m.bottom = Math.min(Math.max(m.bottom, 0), 0.4);
        if (m.top + m.bottom > 0.6) {
            var over = (m.top + m.bottom - 0.6) / 2;
            m.top = Math.max(m.top - over, 0);
            m.bottom = Math.max(m.bottom - over, 0);
        }

        try {
            // Through the chart options rather than priceScale('right').applyOptions: this is the
            // documented route to the visible right hand scale, and visible is restated so the
            // axis can never be dropped by an option merge.
            this._charts.main.chart.applyOptions({
                rightPriceScale: {
                    visible: true,
                    scaleMargins: { top: m.top, bottom: m.bottom },
                },
            });
        }
        catch (e) { }
    },

    resetPriceScale: function () {
        this._priceMargins = { top: 0.2, bottom: 0.1 };
        this._applyPriceMargins();
    },

    _attachVerticalControl: function () {
        var self = this;
        var main = this._charts.main;
        if (!main) return;

        var container = main.container;
        if (this._verticalHandlers) {
            container.removeEventListener('wheel', this._verticalHandlers.wheel, true);
            container.removeEventListener('mousedown', this._verticalHandlers.down, true);
            container.removeEventListener('mousemove', this._verticalHandlers.move, true);
            container.removeEventListener('mouseup', this._verticalHandlers.up, true);
            container.removeEventListener('mouseleave', this._verticalHandlers.up, true);
            container.removeEventListener('dblclick', this._verticalHandlers.dbl, true);
        }

        // Ctrl+wheel = vertical zoom, the same reflex TradingView trains
        var onWheel = function (event) {
            if (!event.ctrlKey) return;
            event.preventDefault();
            event.stopPropagation();

            var step = event.deltaY > 0 ? 0.02 : -0.02;  // wheel down = zoom out
            self._priceMargins.top += step;
            self._priceMargins.bottom += step;
            self._applyPriceMargins();
        };

        var panning = false;
        var lastY = 0;

        var onDown = function (event) {
            if (event.button !== 0 || !event.ctrlKey) return;
            panning = true;
            lastY = event.clientY;
            event.preventDefault();
            event.stopPropagation();
        };

        var onMove = function (event) {
            if (!panning) return;

            var dy = event.clientY - lastY;
            lastY = event.clientY;

            // A drag down moves the data down: more room on top, less at the bottom
            var frac = dy / Math.max(container.clientHeight, 1);
            self._priceMargins.top += frac;
            self._priceMargins.bottom -= frac;
            self._applyPriceMargins();

            event.preventDefault();
            event.stopPropagation();
        };

        var onUp = function () { panning = false; };

        // Ctrl+double click puts the price scale back the way it started
        var onDblClick = function (event) {
            if (!event.ctrlKey) return;
            event.preventDefault();
            self.resetPriceScale();
        };

        container.addEventListener('wheel', onWheel, { capture: true, passive: false });
        container.addEventListener('mousedown', onDown, true);
        container.addEventListener('mousemove', onMove, true);
        container.addEventListener('mouseup', onUp, true);
        container.addEventListener('mouseleave', onUp, true);
        container.addEventListener('dblclick', onDblClick, true);

        this._verticalHandlers = { wheel: onWheel, down: onDown, move: onMove, up: onUp, dbl: onDblClick };
    },

    _attachMeasureTool: function () {
        var self = this;
        var main = this._charts.main;
        if (!main) return;

        this._measurePrimitive = new this._MeasurePrimitive(function (v) {
            return self._formatMeasurePrice(v);
        });
        main.series.candles.attachPrimitive(this._measurePrimitive);

        var container = main.container;

        // init() can run again (theme switch), and the container div outlives the chart. Without
        // this the handlers would stack up and every drag would be processed several times.
        if (this._measureHandlers) {
            var old = this._measureHandlers;
            container.removeEventListener('mousedown', old.down, true);
            container.removeEventListener('mousemove', old.move, true);
            container.removeEventListener('mouseup', old.up, true);
            container.removeEventListener('mouseleave', old.up, true);
            document.removeEventListener('keydown', old.key);
        }

        var dragging = false;
        var start = null;
        var moved = false;

        // Pixel position inside the chart, and the time/price under it
        var pointAt = function (event) {
            var rect = container.getBoundingClientRect();
            var x = event.clientX - rect.left;
            var y = event.clientY - rect.top;

            var price = main.series.candles.coordinateToPrice(y);
            var time = main.chart.timeScale().coordinateToTime(x);
            if (price === null) return null;

            return { x: x, y: y, price: price, time: time };
        };

        // Shift+drag measures, like TradingView. Without shift the mouse keeps its normal job of
        // panning and zooming the chart.
        var onMouseDown = function (event) {
            if (event.button !== 0 || event.ctrlKey) return;

            if (!event.shiftKey) {
                // A plain click wipes a measurement that is still on screen
                if (self._measurePrimitive && self._measurePrimitive._measure)
                    self.clearMeasure();
                return;
            }

            var point = pointAt(event);
            if (!point) return;

            dragging = true;
            moved = false;
            start = point;
            event.preventDefault();
            event.stopPropagation();

            // The chart pans on drag; suspend that while measuring
            main.chart.applyOptions({ handleScroll: false, handleScale: false });
            self._interactionSuspended = true;
        };

        var onMouseMove = function (event) {
            if (!dragging || !start) return;

            var point = pointAt(event);
            if (!point) return;

            moved = true;

            var bars = 0;
            if (self._lastCandles && start.time !== null && point.time !== null) {
                var lo = Math.min(start.time, point.time);
                var hi = Math.max(start.time, point.time);
                for (var i = 0; i < self._lastCandles.length; i++) {
                    var t = self._lastCandles[i].time;
                    if (t >= lo && t <= hi) bars++;
                }
                if (bars > 0) bars -= 1; // spans between candles, not the candles themselves
            }

            self._measurePrimitive.setMeasure({
                active: true,
                x1: start.x, x2: point.x,
                price1: start.price, price2: point.price,
                bars: bars,
                span: (start.time !== null && point.time !== null)
                    ? self._formatSpan(point.time - start.time) : '',
            });

            event.preventDefault();
        };

        var onMouseUp = function () {
            if (!dragging) return;
            dragging = false;
            start = null;

            // A shift-click without dragging clears instead of leaving a dot behind
            if (!moved)
                self.clearMeasure();
            moved = false;

            // Restore the FULL option objects. Handing back plain booleans silently dropped
            // axisPressedMouseMove.price, which is what makes the price axis draggable - after one
            // measurement the chart could only be moved sideways.
            if (self._interactionSuspended) {
                self._interactionSuspended = false;
                var opts = self._chartOptions(false);
                main.chart.applyOptions({
                    handleScroll: opts.handleScroll,
                    handleScale: opts.handleScale,
                });
            }
        };

        var onKeyDown = function (event) {
            if (event.key === 'Escape')
                self.clearMeasure();
        };

        container.addEventListener('mousedown', onMouseDown, true);
        container.addEventListener('mousemove', onMouseMove, true);
        container.addEventListener('mouseup', onMouseUp, true);
        container.addEventListener('mouseleave', onMouseUp, true);
        document.addEventListener('keydown', onKeyDown);

        this._measureHandlers = {
            down: onMouseDown, move: onMouseMove, up: onMouseUp, key: onKeyDown,
        };
    },

    _chartOptions: function (hideTimeScale) {
        var d = this._isDark;
        return {
            layout: {
                background: { type: 'solid', color: d ? '#0a0a0a' : '#fbfbfb' },
                textColor: d ? '#e0e0e0' : '#1a1a1a',
            },
            grid: {
                vertLines: { color: d ? '#1a1a1a' : '#e8e8e8' },
                horzLines: { color: d ? '#1a1a1a' : '#e8e8e8' },
            },
            crosshair: { mode: LightweightCharts.CrosshairMode.Normal },
            timeScale: {
                borderColor: d ? '#333' : '#ccc',
                timeVisible: true,
                secondsVisible: false,
                visible: !hideTimeScale,
            },
            rightPriceScale: {
                borderColor: d ? '#333' : '#ccc',
                // Left on, so the chart fits its candles when it opens. Dragging the price axis
                // switches it off by itself and the manual range then sticks.
                autoScale: true,
            },
            // Spelled out rather than left to the defaults: the measure tool switches these off
            // while dragging, and restoring them as plain booleans quietly dropped the price-axis
            // part of the scaling.
            handleScroll: {
                mouseWheel: true,
                pressedMouseMove: true,
                horzTouchDrag: true,
                vertTouchDrag: true,
            },
            handleScale: {
                mouseWheel: true,
                pinch: true,
                axisPressedMouseMove: { time: true, price: true },
                axisDoubleClickReset: { time: true, price: true },
            },
        };
    },

    _createMainChart: function () {
        var container = document.getElementById('chart-main');
        if (!container) return;

        var chart = LightweightCharts.createChart(container, Object.assign(
            this._chartOptions(false),
            { width: container.clientWidth, height: container.clientHeight }
        ));

        var candleSeries = chart.addCandlestickSeries({
            upColor: '#22c55e', downColor: '#f0616d',
            borderDownColor: '#f0616d', borderUpColor: '#22c55e',
            wickDownColor: '#f0616d', wickUpColor: '#22c55e',
        });

        new ResizeObserver(function () {
            chart.applyOptions({ width: container.clientWidth, height: container.clientHeight });
        }).observe(container);

        this._charts.main = {
            chart: chart,
            container: container,
            series: { candles: candleSeries },
            overlays: {},
            priceLines: [],
        };

        // Feed the OHLCV read-out above the chart. Written straight into the DOM instead of
        // through a Blazor round trip: this fires on every mouse move over the chart.
        var self = this;
        chart.subscribeCrosshairMove(function (param) {
            // Looked up by time in the data we sent, not through param.seriesData: the candlestick
            // series only hands back open/high/low/close, and the volume has to come along too.
            var candle = param && param.time !== undefined
                ? self._candleAt(param.time)
                : null;

            // Off the chart the last candle is shown again, so the bar is never empty
            self._renderOhlcv(candle || self._lastCandle());
        });
    },

    _lastCandle: function () {
        var list = this._lastCandles;
        return list && list.length > 0 ? list[list.length - 1] : null;
    },

    _candleAt: function (time) {
        var list = this._lastCandles;
        if (!list) return null;

        // Binary search: the crosshair fires on every mouse move and the list can hold thousands
        var lo = 0, hi = list.length - 1;
        while (lo <= hi) {
            var mid = (lo + hi) >> 1;
            if (list[mid].time === time) return list[mid];
            if (list[mid].time < time) lo = mid + 1; else hi = mid - 1;
        }
        return null;
    },

    _ohlcvDecimals: 2,

    setPriceDecimals: function (decimals) {
        this._ohlcvDecimals = typeof decimals === 'number' && decimals >= 0 ? decimals : 2;
        this._applyPriceFormat();
    },

    // Tell the series how precise its prices are. Without this every series keeps the library
    // default of two decimals, so on a coin around 0.1883 every axis label rounded to "0.19";
    // the price scale drops duplicate labels and what was left looked like an empty axis.
    _applyPriceFormat: function () {
        var main = this._charts.main;
        if (!main) return;

        var d = this._ohlcvDecimals;
        var format = {
            type: 'price',
            precision: d,
            minMove: Math.pow(10, -d),
        };

        try { main.series.candles.applyOptions({ priceFormat: format }); } catch (e) { }

        // The overlays share the same scale, so they need the same precision or their own
        // last-value labels would disagree with the axis
        Object.keys(main.overlays).forEach(function (key) {
            try { main.overlays[key].applyOptions({ priceFormat: format }); } catch (e) { }
        });
    },

    _formatVolume: function (value) {
        if (!value) return '0';
        if (value >= 1e9) return (value / 1e9).toFixed(2) + 'B';
        if (value >= 1e6) return (value / 1e6).toFixed(2) + 'M';
        if (value >= 1e3) return (value / 1e3).toFixed(2) + 'K';
        return value.toFixed(0);
    },

    _renderOhlcv: function (candle) {
        var host = document.getElementById('chart-ohlcv');
        if (!host) return;

        if (!candle) {
            host.textContent = '';
            return;
        }

        var d = this._ohlcvDecimals;
        var rising = candle.close >= candle.open;
        var cls = rising ? 'ohlcv-up' : 'ohlcv-down';

        var parts = [
            ['O', candle.open.toFixed(d)],
            ['H', candle.high.toFixed(d)],
            ['L', candle.low.toFixed(d)],
            ['C', candle.close.toFixed(d)],
        ];

        var html = '';
        for (var i = 0; i < parts.length; i++)
            html += '<span class="ohlcv-key">' + parts[i][0] + '</span>'
                  + '<span class="ohlcv-value ' + cls + '">' + parts[i][1] + '</span>';

        if (candle.volume !== undefined && candle.volume !== null) {
            html += '<span class="ohlcv-key">V</span>'
                  + '<span class="ohlcv-value">' + this._formatVolume(candle.volume) + '</span>';
        }

        host.innerHTML = html;
    },

    _createSubChart: function (containerId, hideTimeScale) {
        var container = document.getElementById(containerId);
        if (!container) return null;

        var chart = LightweightCharts.createChart(container, Object.assign(
            this._chartOptions(hideTimeScale),
            { width: container.clientWidth, height: container.clientHeight }
        ));

        new ResizeObserver(function () {
            chart.applyOptions({ width: container.clientWidth, height: container.clientHeight });
        }).observe(container);

        this._syncTimeScale(chart);
        return { chart: chart, container: container, series: {} };
    },

    _syncTimeScale: function (subChart) {
        var self = this;
        var mainChart = this._charts.main.chart;

        mainChart.timeScale().subscribeVisibleLogicalRangeChange(function (range) {
            if (self._syncing || !range) return;
            self._syncing = true;
            try { subChart.timeScale().setVisibleLogicalRange(range); } catch (e) { }
            self._syncing = false;
        });

        subChart.timeScale().subscribeVisibleLogicalRangeChange(function (range) {
            if (self._syncing || !range) return;

            // A sub-chart created during setData fits itself to its own data and reports that
            // here. Following it would drag the main chart to wherever this panel happens to sit,
            // which is not something the user did. Only follow a sub-chart the user scrolled.
            if (self._settingData) return;

            self._syncing = true;
            try { mainChart.timeScale().setVisibleLogicalRange(range); } catch (e) { }
            self._syncing = false;
        });
    },

    _addPriceLine: function (series, price, color, lineStyle, title) {
        return series.createPriceLine({
            price: price,
            color: color,
            lineWidth: 1,
            lineStyle: lineStyle === undefined ? 2 : lineStyle,
            axisLabelVisible: true,
            title: title || '',
        });
    },

    _overlayStyles: {
        bbUpper:        { color: '#2196F3', lineWidth: 1, lineStyle: 0 },
        bbMiddle:       { color: '#2196F3', lineWidth: 1, lineStyle: 2 },
        bbLower:        { color: '#2196F3', lineWidth: 1, lineStyle: 0 },
        sma200:         { color: '#e53935', lineWidth: 2, lineStyle: 0 },
        sma50:          { color: '#ff9800', lineWidth: 2, lineStyle: 0 },
        sma20:          { color: '#4caf50', lineWidth: 1, lineStyle: 0 },
        psar:           { color: '#ffeb3b', lineWidth: 0, lineStyle: 0, dots: true },
        keltnerUpper:   { color: '#ab47bc', lineWidth: 1, lineStyle: 0 },
        keltnerMiddle:  { color: '#ab47bc', lineWidth: 1, lineStyle: 2 },
        keltnerLower:   { color: '#ab47bc', lineWidth: 1, lineStyle: 0 },

        nweUpper:       { color: '#9e9e9e', lineWidth: 1, lineStyle: 0 },
        nweMiddle:      { color: '#757575', lineWidth: 1, lineStyle: 2 },
        nweLower:       { color: '#9e9e9e', lineWidth: 1, lineStyle: 0 },

        // Repainting NWE variant, drawn dashed so it is distinguishable from the fixed one
        nweRepaintUpper:  { color: '#8d6e63', lineWidth: 1, lineStyle: 2 },
        nweRepaintMiddle: { color: '#6d4c41', lineWidth: 1, lineStyle: 2 },
        nweRepaintLower:  { color: '#8d6e63', lineWidth: 1, lineStyle: 2 },

        atrRbUpper:     { color: '#90a4ae', lineWidth: 1, lineStyle: 0 },
        atrRbLower:     { color: '#90a4ae', lineWidth: 1, lineStyle: 0 },
        atrRbBasis:     { color: '#42a5f5', lineWidth: 1, lineStyle: 2 },

        vbsUpper:       { color: '#26a69a', lineWidth: 1, lineStyle: 0 },
        vbsLower:       { color: '#26a69a', lineWidth: 1, lineStyle: 0 },
        vbsBasis:       { color: '#9e9e9e', lineWidth: 1, lineStyle: 2 },

        dbrUpper:       { color: '#bdbdbd', lineWidth: 1, lineStyle: 0 },
        dbrLower:       { color: '#bdbdbd', lineWidth: 1, lineStyle: 0 },

        bbmaWma5High:   { color: '#c62828', lineWidth: 1, lineStyle: 0 },
        bbmaWma10High:  { color: '#c62828', lineWidth: 1, lineStyle: 2 },
        bbmaWma5Low:    { color: '#2e7d32', lineWidth: 1, lineStyle: 0 },
        bbmaWma10Low:   { color: '#2e7d32', lineWidth: 1, lineStyle: 2 },
        bbmaEma50:      { color: '#ef6c00', lineWidth: 2, lineStyle: 0 },

        zigzag:         { color: '#ffffff', lineWidth: 1, lineStyle: 0 },
        fibZigzag:      { color: '#ffeb3b', lineWidth: 1, lineStyle: 2 },
    },

    // User-configured styles, keyed the same way as _overlayStyles. Whatever is in here wins over
    // the built-in defaults above, so a series only needs an entry when it was actually changed.
    _userStyles: {},

    setStyles: function (styles) {
        this._userStyles = styles || {};
    },

    _styleFor: function (key) {
        var user = this._userStyles[key];
        if (user)
            return user;
        return this._overlayStyles[key] || { color: '#888', lineWidth: 1, lineStyle: 0 };
    },

    // lightweight-charts renders every UTCTimestamp in UTC, so a candle that opened at 16:00 here
    // was labelled 14:00. Reformatting only the labels would still leave the day separators on UTC
    // midnight, so instead every timestamp entering the widget is shifted into local time once,
    // right here. Axis, crosshair, markers, overlays and the measure tool then all work in the
    // same space, and nothing is ever sent back to C# — the C# side keeps its own UTC values.
    //
    // The offset is read per timestamp rather than once for the whole chart, so a range that spans
    // a daylight saving change keeps every candle on the wall clock time it actually had.
    _toLocalTime: function (time) {
        return time - new Date(time * 1000).getTimezoneOffset() * 60;
    },

    _localizeTimes: function (node) {
        if (!node || typeof node !== 'object')
            return node;

        if (Array.isArray(node)) {
            for (var i = 0; i < node.length; i++)
                this._localizeTimes(node[i]);
            return node;
        }

        for (var key in node) {
            if (!Object.prototype.hasOwnProperty.call(node, key)) continue;

            var value = node[key];
            if ((key === 'time' || key === 'time1' || key === 'time2') && typeof value === 'number')
                node[key] = this._toLocalTime(value);
            else if (value && typeof value === 'object')
                this._localizeTimes(value);
        }
        return node;
    },

    setData: function (candles, overlays, panels, extras) {
        if (!this._loaded || !this._charts.main) return;

        this._localizeTimes(candles);
        this._localizeTimes(overlays);
        this._localizeTimes(panels);
        this._localizeTimes(extras);

        var self = this;
        var mainEntry = this._charts.main;

        // While this runs, the sub-charts are destroyed and rebuilt. A fresh sub-chart fits itself
        // to its own data and reports that through subscribeVisibleLogicalRangeChange, and the
        // two-way sync then pushes that range onto the main chart. Those callbacks arrive after
        // setData has returned, so the plain _syncing flag never caught them.
        this._settingData = true;

        // Remember where the user was looking, so a toggle does not move the chart.
        //
        // In TIME, not in bar indices. A logical range is an offset from the first bar, and the
        // first bar moves: the scanner trims old candles as new ones come in, so between two
        // refreshes the whole series can shift. Restoring index 800..930 onto a series that just
        // lost a few hundred bars at the front lands past the end - an empty chart with the
        // candles pushed off to the left, which is exactly the jump that kept happening.
        // Times survive that. The logical range is kept only as a fallback for when the window
        // holds no bars at all and getVisibleRange returns nothing.
        var visibleRange = null;
        var visibleTimeRange = null;
        try {
            visibleRange = mainEntry.chart.timeScale().getVisibleLogicalRange();
            visibleTimeRange = mainEntry.chart.timeScale().getVisibleRange();
        } catch (e) { }

        mainEntry.series.candles.setData(candles);
        this._lastCandles = candles;

        // Show the newest candle until the mouse moves over the chart
        this._renderOhlcv(this._lastCandle());

        // Time window the candles cover. Overlay points and markers outside it are dropped:
        // a single stray point (an indicator computed over a longer history than the visible
        // snapshot) would otherwise stretch the time scale so far that the candles shrink to
        // an invisible sliver, which looked like "the candles disappeared" when toggling an overlay.
        var minTime = null, maxTime = null;
        if (candles && candles.length > 0) {
            minTime = candles[0].time;
            maxTime = candles[candles.length - 1].time;
        }
        var inRange = function (time) {
            return minTime === null || (time >= minTime && time <= maxTime);
        };

        // Candle times, so markers can be snapped onto an existing bar. lightweight-charts
        // rejects a marker whose time is not in the series data and then stops rendering them.
        var candleTimes = {};
        if (candles) {
            for (var i = 0; i < candles.length; i++)
                candleTimes[candles[i].time] = true;
        }

        // Remove old overlays
        Object.keys(mainEntry.overlays).forEach(function (key) {
            try { mainEntry.chart.removeSeries(mainEntry.overlays[key]); } catch (e) { }
        });
        mainEntry.overlays = {};

        // Remove old price lines (positions)
        mainEntry.priceLines.forEach(function (line) {
            try { mainEntry.series.candles.removePriceLine(line); } catch (e) { }
        });
        mainEntry.priceLines = [];

        // Add new overlays
        if (overlays) {
            Object.keys(overlays).forEach(function (key) {
                var data = overlays[key];
                if (!data || data.length === 0) return;

                data = data.filter(function (p) { return inRange(p.time); });
                if (data.length === 0) return;

                var style = self._styleFor(key);

                var series = mainEntry.chart.addLineSeries({
                    color: style.color,
                    // lineVisible, not lineWidth: 0. lightweight-charts only accepts 1..4 there and
                    // silently falls back to its default of 3 for anything else, which is why the
                    // Parabolic SAR still drew a line straight through its dots.
                    lineVisible: !style.dots,
                    lineWidth: style.lineWidth || 1,
                    lineStyle: style.lineStyle,
                    pointMarkersVisible: !!style.dots,
                    pointMarkersRadius: style.dots ? 2 : 0,
                    crosshairMarkerVisible: false,
                    lastValueVisible: false,
                    priceLineVisible: false,
                    title: '',
                });
                series.setData(data);
                mainEntry.overlays[key] = series;
            });
        }

        // Overlays were just recreated with the library default precision
        this._applyPriceFormat();

        // Zones as rectangles behind the candles
        this._applyZones(extras && extras.zones ? extras.zones : []);

        // Signals and position fills as markers on the candle series. Only markers that land on
        // an actual bar survive — a signal's close time does not always align with a candle open.
        var markers = [];
        if (extras && extras.markers)
            markers = extras.markers.filter(function (m) { return candleTimes[m.time] === true; });
        markers.sort(function (a, b) { return a.time - b.time; });
        try { mainEntry.series.candles.setMarkers(markers); } catch (e) { }

        // Position levels: bounded segments with a caption, not full-width price lines
        this._applySegments(extras && extras.segments ? extras.segments : []);
        if (this._segmentPrimitive) {
            this._segmentPrimitive.setLabels(extras && extras.labels ? extras.labels : []);
            this._segmentPrimitive.setDots(extras && extras.dots ? extras.dots : []);
        }

        // Anything that genuinely spans the chart (the FIB levels) stays a price line
        if (extras && extras.priceLines) {
            extras.priceLines.forEach(function (pl) {
                var line = self._addPriceLine(
                    mainEntry.series.candles, pl.price, pl.color, pl.lineStyle, pl.title);
                mainEntry.priceLines.push(line);
            });
        }

        // Remove old sub-charts (except main)
        Object.keys(this._charts).forEach(function (key) {
            if (key === 'main') return;
            try { self._charts[key].chart.remove(); } catch (e) { }
            delete self._charts[key];
        });

        // Create sub-panels
        if (panels) {
            if (panels.volume) this._createVolumePanel(panels.volume);
            // RSI, stochastic and Lux share one panel — they are all bounded oscillators,
            // so a single pane saves a lot of vertical space
            if (panels.rsi || panels.stoch || panels.lux)
                this._createOscillatorPanel(panels.rsi, panels.stoch, panels.lux);
            if (panels.macd) this._createMacdPanel(panels.macd);
        }

        // Only reset the view when the chart shows something else than before. Fitting on every
        // call threw away the zoom and scroll position on each overlay toggle.
        if (this._pendingFit) {
            this._pendingFit = false;
            this._zoomLast(candles ? candles.length : 0);
        }
        else if (visibleTimeRange) {
            try { mainEntry.chart.timeScale().setVisibleRange(visibleTimeRange); }
            catch (e) {
                try { mainEntry.chart.timeScale().setVisibleLogicalRange(visibleRange); } catch (e2) { }
            }
        }
        else if (visibleRange) {
            try { mainEntry.chart.timeScale().setVisibleLogicalRange(visibleRange); } catch (e) { }
        }

        // Release the sync guard only once the sub-charts have settled. Their range callbacks are
        // queued, not immediate, so clearing it here and now would let them through after all.
        setTimeout(function () { self._settingData = false; }, 0);
    },

    // How many candles the initial view shows. fitContent squeezed the whole loaded history into
    // the pane, which left the candles as hair-thin slivers; the Avalonia chart zooms to the last
    // ZonesDlz.CandleCountZoom candles instead.
    _zoomCandles: 125,

    setZoomCandles: function (count) {
        if (typeof count === 'number' && count > 0)
            this._zoomCandles = count;
    },

    _zoomLast: function (candleCount) {
        var timeScale = this._charts.main.chart.timeScale();
        if (candleCount <= 0) {
            timeScale.fitContent();
            return;
        }

        var count = Math.min(this._zoomCandles, candleCount);
        try {
            // A few bars of empty space on the right, the way a trading chart normally sits
            timeScale.setVisibleLogicalRange({ from: candleCount - count, to: candleCount + 2 });
        }
        catch (e) {
            timeScale.fitContent();
        }
    },

    /// Ask the next setData to zoom to the most recent candles again (symbol or interval changed).
    resetView: function () {
        this._pendingFit = true;
    },

    _applySegments: function (segments) {
        var mainEntry = this._charts.main;
        if (!this._segmentPrimitive) {
            this._segmentPrimitive = new this._SegmentPrimitive(segments);
            mainEntry.series.candles.attachPrimitive(this._segmentPrimitive);
        }
        else {
            this._segmentPrimitive.setSegments(segments);
        }
    },

    _applyZones: function (zones) {
        var mainEntry = this._charts.main;
        if (!this._zonePrimitive) {
            this._zonePrimitive = new this._RectanglePrimitive(zones);
            mainEntry.series.candles.attachPrimitive(this._zonePrimitive);
        }
        else {
            this._zonePrimitive.setRects(zones);
        }
    },

    _createVolumePanel: function (candles) {
        var entry = this._createSubChart('chart-volume', true);
        if (!entry) return;

        var up = this._styleFor('volumeUp').color;
        var down = this._styleFor('volumeDown').color;

        var series = entry.chart.addHistogramSeries({
            priceFormat: { type: 'volume' },
        });
        series.setData(candles.map(function (c) {
            return {
                time: c.time,
                value: c.volume || 0,
                color: c.close >= c.open ? up : down,
            };
        }));
        entry.series.volume = series;
        this._charts.volume = entry;
    },

    _createOscillatorPanel: function (rsiData, stochData, luxData) {
        var entry = this._createSubChart('chart-oscillator', true);
        if (!entry) return;

        var levelSeries = null;

        if (rsiData) {
            var rsiStyle = this._styleFor('rsi');
            var rsiSeries = entry.chart.addLineSeries({
                color: rsiStyle.color, lineWidth: rsiStyle.lineWidth, lineStyle: rsiStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, title: 'RSI',
            });
            rsiSeries.setData(rsiData.data);
            entry.series.rsi = rsiSeries;
            levelSeries = rsiSeries;

            var rsiOs = this._styleFor('rsiOversold');
            var rsiOb = this._styleFor('rsiOverbought');
            this._addPriceLine(rsiSeries, rsiData.oversold, rsiOs.color, rsiOs.lineStyle);
            this._addPriceLine(rsiSeries, rsiData.overbought, rsiOb.color, rsiOb.lineStyle);
        }

        if (stochData) {
            var kStyle = this._styleFor('stochK');
            var kSeries = entry.chart.addLineSeries({
                color: kStyle.color, lineWidth: kStyle.lineWidth, lineStyle: kStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, title: '%K',
            });
            kSeries.setData(stochData.k);

            var dStyle = this._styleFor('stochD');
            var dSeries = entry.chart.addLineSeries({
                color: dStyle.color, lineWidth: dStyle.lineWidth, lineStyle: dStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, title: '%D',
            });
            dSeries.setData(stochData.d);

            entry.series.k = kSeries;
            entry.series.d = dSeries;

            // Only draw the threshold lines once, RSI and stochastic share the 0..100 scale
            if (!levelSeries) {
                var stochOs = this._styleFor('stochOversold');
                var stochOb = this._styleFor('stochOverbought');
                this._addPriceLine(kSeries, stochData.oversold, stochOs.color, stochOs.lineStyle);
                this._addPriceLine(kSeries, stochData.overbought, stochOb.color, stochOb.lineStyle);
                levelSeries = kSeries;
            }
        }

        if (luxData) {
            // Lux counts are unbounded, so it gets its own overlay scale to avoid
            // squashing the 0..100 oscillators sharing this pane
            var osSeries = entry.chart.addHistogramSeries({
                color: this._styleFor('luxOversold').color,
                lastValueVisible: false, priceLineVisible: false,
                priceScaleId: 'lux',
            });
            osSeries.setData(luxData.map(function (d) {
                return { time: d.time, value: d.oversold };
            }));

            var obSeries = entry.chart.addHistogramSeries({
                color: this._styleFor('luxOverbought').color,
                lastValueVisible: false, priceLineVisible: false,
                priceScaleId: 'lux',
            });
            obSeries.setData(luxData.map(function (d) {
                return { time: d.time, value: -d.overbought };
            }));

            entry.chart.priceScale('lux').applyOptions({
                scaleMargins: { top: 0.75, bottom: 0 },
            });

            entry.series.luxOversold = osSeries;
            entry.series.luxOverbought = obSeries;
        }

        this._charts.oscillator = entry;
    },

    _createMacdPanel: function (macdData) {
        var entry = this._createSubChart('chart-macd', true);
        if (!entry) return;

        var histSeries = entry.chart.addHistogramSeries({
            lastValueVisible: false, priceLineVisible: false,
        });
        histSeries.setData(macdData.histogram);

        var macdStyle = this._styleFor('macdLine');
        var macdSeries = entry.chart.addLineSeries({
            color: macdStyle.color, lineWidth: macdStyle.lineWidth, lineStyle: macdStyle.lineStyle,
            lastValueVisible: true, priceLineVisible: false, title: 'MACD',
        });
        macdSeries.setData(macdData.macdLine);

        var signalStyle = this._styleFor('macdSignal');
        var signalSeries = entry.chart.addLineSeries({
            color: signalStyle.color, lineWidth: signalStyle.lineWidth, lineStyle: signalStyle.lineStyle,
            lastValueVisible: true, priceLineVisible: false, title: 'Signal',
        });
        signalSeries.setData(macdData.signal);

        this._addPriceLine(macdSeries, 0, 'rgba(150,150,150,0.3)');

        entry.series.histogram = histSeries;
        entry.series.macd = macdSeries;
        entry.series.signal = signalSeries;
        this._charts.macd = entry;
    },

    addMarkers: function (markers) {
        if (!this._loaded || !this._charts.main) return;
        this._charts.main.series.candles.setMarkers(markers);
    },

    dispose: function () {
        var self = this;
        Object.keys(this._charts).forEach(function (key) {
            try { self._charts[key].chart.remove(); } catch (e) { }
        });
        this._charts = {};
        this._zonePrimitive = null;
        this._segmentPrimitive = null;
        this._measurePrimitive = null;
        this._lastCandles = null;
        this._loaded = false;
    }
};

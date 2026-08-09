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
                        if (y1 === null || y2 === null) return;

                        var x1 = edgeX(r.time1, 0);
                        // A zone that is still active runs to the right edge of the chart
                        var x2 = edgeX(r.time2, widthPx);

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

                    ctx.fillStyle = '#ffffff';
                    ctx.textBaseline = 'top';
                    for (var j = 0; j < lines.length; j++)
                        ctx.fillText(lines[j], boxX + padding, boxY + padding / 2 + j * lineHeight);
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

window.ChartWidget = {
    _charts: {},
    _isDark: true,
    _loaded: false,
    _syncing: false,
    _zonePrimitive: null,
    _RectanglePrimitive: null,
    _MeasurePrimitive: null,
    _measurePrimitive: null,
    _measureArmed: false,
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
        this._MeasurePrimitive = createMeasurePrimitive();
        this.dispose();
        this._createMainChart();
        this._attachMeasureTool();
        this._loaded = true;
    },

    /// Turn the measuring tool on or off. Shift+drag measures regardless of this setting.
    setMeasureMode: function (enabled) {
        this._measureArmed = !!enabled;
        if (!enabled)
            this.clearMeasure();

        var main = this._charts.main;
        if (main && main.container)
            main.container.style.cursor = enabled ? 'crosshair' : '';
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

    _attachMeasureTool: function () {
        var self = this;
        var main = this._charts.main;
        if (!main) return;

        this._measurePrimitive = new this._MeasurePrimitive(function (v) {
            return self._formatMeasurePrice(v);
        });
        main.series.candles.attachPrimitive(this._measurePrimitive);

        var container = main.container;
        var dragging = false;
        var start = null;

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

        container.addEventListener('mousedown', function (event) {
            if (event.button !== 0) return;
            if (!self._measureArmed && !event.shiftKey) return;

            var point = pointAt(event);
            if (!point) return;

            dragging = true;
            start = point;
            event.preventDefault();
            event.stopPropagation();

            // The chart pans on drag; suspend that while measuring
            main.chart.applyOptions({ handleScroll: false, handleScale: false });
        }, true);

        container.addEventListener('mousemove', function (event) {
            if (!dragging || !start) return;

            var point = pointAt(event);
            if (!point) return;

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
        }, true);

        var endDrag = function () {
            if (!dragging) return;
            dragging = false;
            start = null;
            // Restore panning; the measurement stays on screen until the next drag or Escape
            main.chart.applyOptions({ handleScroll: true, handleScale: true });
        };

        container.addEventListener('mouseup', endDrag, true);
        container.addEventListener('mouseleave', endDrag, true);

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape')
                self.clearMeasure();
        });
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
            upColor: '#45c173', downColor: '#cd4040',
            borderDownColor: '#cd4040', borderUpColor: '#45c173',
            wickDownColor: '#cd4040', wickUpColor: '#45c173',
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

    setData: function (candles, overlays, panels, extras) {
        if (!this._loaded || !this._charts.main) return;

        var self = this;
        var mainEntry = this._charts.main;

        // Remember where the user was looking, so a toggle does not move the chart
        var visibleRange = null;
        try { visibleRange = mainEntry.chart.timeScale().getVisibleLogicalRange(); } catch (e) { }

        mainEntry.series.candles.setData(candles);
        this._lastCandles = candles;

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

                var style = self._overlayStyles[key] || { color: '#888', lineWidth: 1, lineStyle: 0 };

                var series = mainEntry.chart.addLineSeries({
                    color: style.color,
                    lineWidth: style.dots ? 0 : style.lineWidth,
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

        // Zones as rectangles behind the candles
        this._applyZones(extras && extras.zones ? extras.zones : []);

        // Signals and position fills as markers on the candle series. Only markers that land on
        // an actual bar survive — a signal's close time does not always align with a candle open.
        var markers = [];
        if (extras && extras.markers)
            markers = extras.markers.filter(function (m) { return candleTimes[m.time] === true; });
        markers.sort(function (a, b) { return a.time - b.time; });
        try { mainEntry.series.candles.setMarkers(markers); } catch (e) { }

        // Position levels as labelled horizontal lines
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
            mainEntry.chart.timeScale().fitContent();
        }
        else if (visibleRange) {
            try { mainEntry.chart.timeScale().setVisibleLogicalRange(visibleRange); } catch (e) { }
        }
    },

    /// Ask the next setData to fit the content again (symbol or interval changed).
    resetView: function () {
        this._pendingFit = true;
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

        var series = entry.chart.addHistogramSeries({
            priceFormat: { type: 'volume' },
        });
        series.setData(candles.map(function (c) {
            return {
                time: c.time,
                value: c.volume || 0,
                color: c.close >= c.open ? 'rgba(69,193,115,0.5)' : 'rgba(205,64,64,0.5)',
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
            var rsiSeries = entry.chart.addLineSeries({
                color: '#ab47bc', lineWidth: 1,
                lastValueVisible: true, priceLineVisible: false, title: 'RSI',
            });
            rsiSeries.setData(rsiData.data);
            entry.series.rsi = rsiSeries;
            levelSeries = rsiSeries;

            this._addPriceLine(rsiSeries, rsiData.oversold, 'rgba(69,193,115,0.4)', 2);
            this._addPriceLine(rsiSeries, rsiData.overbought, 'rgba(205,64,64,0.4)', 2);
        }

        if (stochData) {
            var kSeries = entry.chart.addLineSeries({
                color: '#2196F3', lineWidth: 1,
                lastValueVisible: true, priceLineVisible: false, title: '%K',
            });
            kSeries.setData(stochData.k);

            var dSeries = entry.chart.addLineSeries({
                color: '#ff9800', lineWidth: 1,
                lastValueVisible: true, priceLineVisible: false, title: '%D',
            });
            dSeries.setData(stochData.d);

            entry.series.k = kSeries;
            entry.series.d = dSeries;

            // Only draw the threshold lines once, RSI and stochastic share the 0..100 scale
            if (!levelSeries) {
                this._addPriceLine(kSeries, stochData.oversold, 'rgba(69,193,115,0.4)', 2);
                this._addPriceLine(kSeries, stochData.overbought, 'rgba(205,64,64,0.4)', 2);
                levelSeries = kSeries;
            }
        }

        if (luxData) {
            // Lux counts are unbounded, so it gets its own overlay scale to avoid
            // squashing the 0..100 oscillators sharing this pane
            var osSeries = entry.chart.addHistogramSeries({
                color: 'rgba(69,193,115,0.55)',
                lastValueVisible: false, priceLineVisible: false,
                priceScaleId: 'lux',
            });
            osSeries.setData(luxData.map(function (d) {
                return { time: d.time, value: d.oversold };
            }));

            var obSeries = entry.chart.addHistogramSeries({
                color: 'rgba(205,64,64,0.55)',
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

        var macdSeries = entry.chart.addLineSeries({
            color: '#2196F3', lineWidth: 1,
            lastValueVisible: true, priceLineVisible: false, title: 'MACD',
        });
        macdSeries.setData(macdData.macdLine);

        var signalSeries = entry.chart.addLineSeries({
            color: '#ff9800', lineWidth: 1,
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
        this._measurePrimitive = null;
        this._lastCandles = null;
        this._loaded = false;
    }
};

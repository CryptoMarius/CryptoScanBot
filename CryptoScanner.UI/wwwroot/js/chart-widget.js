// The markers a plugin overlay asks for arrive as a single character - the triangle of an entry,
// the circle of a break - because a caption is what the overlay contract carries. Drawn as TEXT
// they depend on the system font: another shape, another baseline, another size than the chart
// they are held against. Drawn as SHAPES they are ours, and they match what TradingView paints.
var MARKER_VORMEN = {
    '▲': 'driehoekOp',
    '▼': 'driehoekNeer',
    '●': 'cirkel',
    '✕': 'kruis',
    '◆': 'ruit',
};

/// Draws one marker centred on (x, y), in device pixels, with a dark rim so a pale marker on a
/// pale background keeps its edge.
function tekenMarker(ctx, soort, x, y, r, kleur) {
    ctx.beginPath();
    if (soort === 'driehoekOp') {
        ctx.moveTo(x, y - r);
        ctx.lineTo(x + r, y + r * 0.8);
        ctx.lineTo(x - r, y + r * 0.8);
        ctx.closePath();
    }
    else if (soort === 'driehoekNeer') {
        ctx.moveTo(x, y + r);
        ctx.lineTo(x + r, y - r * 0.8);
        ctx.lineTo(x - r, y - r * 0.8);
        ctx.closePath();
    }
    else if (soort === 'ruit') {
        ctx.moveTo(x, y - r);
        ctx.lineTo(x + r, y);
        ctx.lineTo(x, y + r);
        ctx.lineTo(x - r, y);
        ctx.closePath();
    }
    else if (soort === 'kruis') {
        // Two strokes, not a filled shape: a cross has no inside.
        var d = r * 0.8;
        ctx.moveTo(x - d, y - d); ctx.lineTo(x + d, y + d);
        ctx.moveTo(x + d, y - d); ctx.lineTo(x - d, y + d);
        ctx.lineWidth = Math.max(2, r * 0.55);
        ctx.strokeStyle = 'rgba(0,0,0,0.75)';
        ctx.stroke();
        ctx.lineWidth = Math.max(1, r * 0.4);
        ctx.strokeStyle = kleur;
        ctx.stroke();
        return;
    }
    else {
        ctx.arc(x, y, r, 0, 2 * Math.PI);
    }
    ctx.lineWidth = Math.max(1.5, r * 0.5);
    ctx.strokeStyle = 'rgba(0,0,0,0.75)';
    ctx.stroke();
    ctx.fillStyle = kleur;
    ctx.fill();
}


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

                            // A KNOWN time, inside the visible window, that timeToCoordinate cannot
                            // map: lightweight-charts only maps timestamps that sit on a bar of the
                            // displayed series, so a zone that closed on a 1h boundary has no
                            // coordinate on a 4h chart. Falling through to `fallback` here painted
                            // such a zone all the way to the RIGHT EDGE - the same picture as a zone
                            // that never broke at all. That is why broken zones looked unbroken in
                            // this chart while the Avalonia one, which puts CloseTime straight onto a
                            // continuous axis, showed them ending where they ended.
                            // Interpolating over the visible range puts the edge back where it
                            // belongs, to within one bar.
                            var fromX = null, toX = null;
                            try {
                                fromX = timeScale.timeToCoordinate(visible.from);
                                toX = timeScale.timeToCoordinate(visible.to);
                            } catch (e) { fromX = null; toX = null; }
                            if (fromX !== null && toX !== null && visible.to > visible.from) {
                                var part = (time - visible.from) / (visible.to - visible.from);
                                return fromX + (toX - fromX) * part;
                            }
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
                            // Same trap as in the rectangle overlay above: a known time inside the
                            // visible window that does not sit on a bar of the displayed series has
                            // no coordinate, and falling through to `fallback` stretched the segment
                            // to the chart edge instead of ending it where it ends.
                            var fromX = null, toX = null;
                            try {
                                fromX = timeScale.timeToCoordinate(visible.from);
                                toX = timeScale.timeToCoordinate(visible.to);
                            } catch (e) { fromX = null; toX = null; }
                            if (fromX !== null && toX !== null && visible.to > visible.from) {
                                var part = (time - visible.from) / (visible.to - visible.from);
                                return fromX + (toX - fromX) * part;
                            }
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
                    //
                    // Several captions can belong to the SAME candle and the same side: VBS sends a
                    // stop-loss and a take-profit line per band break. Those were both drawn at the
                    // very same spot, so the skip below dropped the second one every single time and
                    // the take-profit line was never on screen. They are stacked outward from the bar
                    // instead - the first one nearest to it, in the order the overlay emitted them -
                    // and the skip then works per candle, so a stack survives or is dropped whole.
                    var lastRight = { above: -1e9, below: -1e9 };
                    var lineHeight = 12 * vRatio;
                    ctx.font = Math.round(10 * vRatio) + 'px sans-serif';

                    var index = 0;
                    while (index < source._labels.length) {
                        // The labels are sorted by time and Array.sort keeps equal elements in their
                        // original order, so one candle's captions sit together in emission order.
                        var first = source._labels[index];
                        var group = [first];
                        while (index + group.length < source._labels.length) {
                            var candidate = source._labels[index + group.length];
                            if (candidate.time !== first.time || !candidate.above !== !first.above)
                                break;
                            group.push(candidate);
                        }
                        index += group.length;

                        // null, not NaN, is what these two return for a price or a time they cannot
                        // place, and isFinite(null) is true - so an unplaceable caption used to be
                        // drawn at coordinate 0 instead of being skipped.
                        var x = null;
                        try { x = timeScale.timeToCoordinate(first.time); } catch (e) { x = null; }
                        if (x === null || !isFinite(x)) continue;

                        // A caption of ONE character is a marker, not a sentence: the triangle of
                        // an entry, the circle of a break, the cross of a line crossing. Those are
                        // drawn bigger, with a dark rim, and they are allowed to sit far closer
                        // together than a caption is.
                        //
                        // The rule below used to reserve the full width plus six pixels for every
                        // caption alike, and everything that did not fit was dropped without a
                        // trace. Zoomed out that is most of them: three break markers on a weekly
                        // chart were invisible until the chart was zoomed in far enough for them to
                        // clear each other. A marker that is not drawn reads as a marker that is
                        // not there, which is the one thing a chart of this must never say.
                        var teken = true;
                        for (var g0 = 0; g0 < group.length; g0++)
                            if (!MARKER_VORMEN[group[g0].text]) teken = false;

                        ctx.font = Math.round(10 * vRatio) + 'px sans-serif';
                        var straal = 5 * vRatio;

                        // Widest line of the stack decides whether it fits next to the previous one
                        var w = 0;
                        for (var g = 0; g < group.length; g++)
                            w = Math.max(w, teken ? 2 * straal : ctx.measureText(group[g].text).width);

                        var left = x * hRatio - w / 2;
                        var lane = first.above ? 'above' : 'below';
                        if (left < lastRight[lane]) continue;
                        // A marker only has to clear the one before it by two pixels; a caption
                        // keeps the old margin, because two of those beside each other are
                        // genuinely unreadable.
                        lastRight[lane] = teken ? left + 2 * hRatio : left + w + 6 * hRatio;

                        ctx.textBaseline = first.above ? 'bottom' : 'top';

                        var previous = null;
                        for (var k = 0; k < group.length; k++) {
                            var l = group[k];
                            var y = source._series.priceToCoordinate(l.price);
                            if (y === null || !isFinite(y)) continue;

                            // Each caption starts at its own price, but never lands on top of the
                            // one before it: overlays that mark the same candle keep their own
                            // anchor while a pair sharing one anchor ends up as two clean lines.
                            var yText = y * vRatio + (first.above ? -8 : 8) * vRatio;
                            if (previous !== null)
                                yText = first.above
                                    ? Math.min(yText, previous - lineHeight)
                                    : Math.max(yText, previous + lineHeight);
                            previous = yText;

                            if (teken) {
                                tekenMarker(ctx, MARKER_VORMEN[l.text], x * hRatio,
                                    yText + (first.above ? -straal : straal), straal,
                                    l.color || '#ffffff');
                            }
                            else {
                                ctx.fillStyle = l.color || '#ffffff';
                                ctx.fillText(l.text,
                                    x * hRatio - ctx.measureText(l.text).width / 2, yText);
                            }
                        }
                    }
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

/// Full-height vertical lines at given times, for the sub-panels.
///
/// The main chart draws its position markers as segments between two prices, which is what keeps
/// them clear of the candle wicks. A sub-panel has no meaningful price to bound them by — the point
/// there is only "this moment", carried down through volume, RSI/stochastic and MACD so the eye can
/// follow one instant across all four panes. So: no prices, top to bottom of whatever pane it is
/// attached to.
function createVerticalPrimitive() {
    class VerticalRenderer {
        constructor(source) { this._source = source; }

        draw(target) {
            var source = this._source;
            if (!source._chart || !source._lines || source._lines.length === 0)
                return;

            try {
                var timeScale = source._chart.timeScale();
                target.useBitmapCoordinateSpace(function (scope) {
                    var ctx = scope.context;
                    var hRatio = scope.horizontalPixelRatio;
                    var vRatio = scope.verticalPixelRatio;
                    var heightPx = scope.mediaSize.height;

                    ctx.save();
                    try {
                        // Same dotted pattern as the vertical open markers on the main chart
                        ctx.setLineDash([2 * hRatio, 4 * hRatio]);
                        ctx.lineWidth = Math.max(1, Math.round(hRatio));

                        source._lines.forEach(function (line) {
                            var x = null;
                            try { x = timeScale.timeToCoordinate(line.time); } catch (e) { x = null; }
                            if (x === null || !isFinite(x)) return;

                            ctx.strokeStyle = line.color || '#888888';
                            ctx.beginPath();
                            ctx.moveTo(x * hRatio, 0);
                            ctx.lineTo(x * hRatio, heightPx * vRatio);
                            ctx.stroke();
                        });
                    }
                    finally {
                        ctx.restore();
                    }
                });
            }
            catch (e) {
                if (window.console) console.error('vertical overlay draw failed', e);
            }
        }
    }

    class VerticalPaneView {
        constructor(source) { this._renderer = new VerticalRenderer(source); }
        renderer() { return this._renderer; }
        // Behind the data: these are reference marks, not something to read a value off
        zOrder() { return 'bottom'; }
    }

    class VerticalPrimitive {
        constructor(lines) {
            this._lines = lines || [];
            this._paneView = new VerticalPaneView(this);
        }
        attached(param) {
            this._chart = param.chart;
            this._series = param.series;
            this._requestUpdate = param.requestUpdate;
        }
        detached() { }
        paneViews() { return [this._paneView]; }
        updateAllViews() { }
        setLines(lines) {
            this._lines = lines || [];
            if (this._requestUpdate) this._requestUpdate();
        }
    }

    return VerticalPrimitive;
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
    _VerticalPrimitive: null,

    // Times the position markers stand at, carried down into the sub-panels. Held here because the
    // panels are rebuilt on every setData and have to be able to ask for them again.
    _verticals: [],
    _measurePrimitive: null,
    _measureHandlers: null,
    _verticalHandlers: null,
    _interactionSuspended: false,
    _pendingFit: true,
    _pendingPriceOnly: false,
    _lastCandles: null,
    _markerLaag: null,
    _markerSerie: null,

    ensureLibrary: function () {
        return new Promise(function (resolve, reject) {
            if (window.LightweightCharts) { resolve(); return; }
            var script = document.createElement('script');
            // From the project, not from unpkg.com. Fetching it over the internet meant no chart
            // at all without a connection, and a trading application that needs a stranger's
            // server to start is not something to leave standing.
            //
            // The path is taken from THIS file's own script tag rather than written out: the
            // Photino host serves these from _content/CryptoScanner.UI/js/ and the web host from
            // somewhere else, and a hard-coded folder would work in one and leave the other
            // without a chart at all.
            var eigen = '_content/CryptoScanner.UI/js/';
            var tags = document.getElementsByTagName('script');
            for (var i = 0; i < tags.length; i++) {
                var bron = tags[i].src || '';
                var k = bron.indexOf('chart-widget.js');
                if (k > 0) { eigen = bron.substring(0, k); break; }
            }
            script.src = eigen + 'lightweight-charts.standalone.production.js';
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
        this._VerticalPrimitive = createVerticalPrimitive();
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

    /// Hands the price axis back to autoScale AND puts the margins back to their default, so the
    /// reload button and the double click on the axis both undo a scale that was dragged out of
    /// range while keeping the stretch of time you were looking at.
    /// Both halves are needed: autoScale on its own leaves the dragged scaleMargins in place.
    resetPriceScale: function () {
        this._priceMargins = { top: 0.2, bottom: 0.1 };
        this._applyPriceMargins();
        if (!this._charts.main) return;
        try { this._charts.main.chart.priceScale('right').applyOptions({ autoScale: true }); } catch (e) { }
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
                background: { type: 'solid', color: d ? '#131316' : '#f0f0f5' },
                textColor: d ? '#e0e0e0' : '#232323',
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

        var candleSeries = chart.addSeries(LightweightCharts.CandlestickSeries, {
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

            // The price under the cursor, as the Avalonia subtitle shows it (parity point B7)
            var price = null;
            if (param && param.point && candle) {
                try { price = candleSeries.coordinateToPrice(param.point.y); } catch (e) { price = null; }
            }

            // Off the chart the last candle is shown again, so the bar is never empty
            self._renderOhlcv(candle || self._lastCandle(), price);

            // The crosshair reaches every pane by itself now: they are panes of THIS chart, not
            // charts of their own that had to be told where the mouse was.
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

    _renderOhlcv: function (candle, price) {
        var host = document.getElementById('chart-ohlcv');
        if (!host) return;

        if (!candle) {
            host.textContent = '';
            return;
        }

        var d = this._ohlcvDecimals;
        var rising = candle.close >= candle.open;
        var cls = rising ? 'ohlcv-up' : 'ohlcv-down';

        // Date of the candle and the price under the cursor, the first half of the
        // Avalonia subtitle: "ddd yyyy-MM-dd HH:mm, price: x (O: .. H: .. L: .. C: .. V: ..)"
        // The times in the widget are already shifted to local time (_toLocalTime), so the UTC
        // getters give the wall clock time.
        var html = '';
        if (typeof candle.time === 'number') {
            var t = new Date(candle.time * 1000);
            var pad = function (n) { return (n < 10 ? '0' : '') + n; };
            var days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
            html += '<span class="ohlcv-value">' + days[t.getUTCDay()] + ' ' + t.getUTCFullYear() + '-' + pad(t.getUTCMonth() + 1)
                  + '-' + pad(t.getUTCDate()) + ' ' + pad(t.getUTCHours()) + ':' + pad(t.getUTCMinutes()) + '</span>';
        }
        if (typeof price === 'number' && isFinite(price)) {
            html += '<span class="ohlcv-key">price</span>'
                  + '<span class="ohlcv-value">' + price.toFixed(d) + '</span>';
        }

        var parts = [
            ['O', candle.open.toFixed(d)],
            ['H', candle.high.toFixed(d)],
            ['L', candle.low.toFixed(d)],
            ['C', candle.close.toFixed(d)],
        ];

        for (var i = 0; i < parts.length; i++)
            html += '<span class="ohlcv-key">' + parts[i][0] + '</span>'
                  + '<span class="ohlcv-value ' + cls + '">' + parts[i][1] + '</span>';

        if (candle.volume !== undefined && candle.volume !== null) {
            html += '<span class="ohlcv-key">V</span>'
                  + '<span class="ohlcv-value">' + this._formatVolume(candle.volume) + '</span>';
        }

        host.innerHTML = html;
    },

    /// A panel is now a PANE of the one chart, not a chart of its own.
    ///
    /// It used to be four charts side by side - candles, volume, oscillator, macd - each with its
    /// own time scale, its own crosshair and its own price axis, kept in step by hand. That cost
    /// six functions and every one of them could come apart: the panes drifting away from each
    /// other on a scroll, a position marker landing on another candle in the volume panel than on
    /// the candles, the crosshair bouncing between four charts, a price axis that measured itself
    /// differently per pane and so gave each one a different plot width. Every one of those was
    /// patched, and the patches are what this replaces.
    ///
    /// A pane cannot drift: there is one time scale for all of them.
    /// How tall each pane is. The panes divide the height between them by weight, so this is the
    /// one place that decides the candles get the room and the panels stay a strip - where three
    /// container heights in the markup used to do it, each of which then had to be measured back
    /// out of the DOM to keep the charts the same size.
    _verdeelPanelen: function (veranderd) {
        var main = this._charts.main;
        if (!main) return;

        var gewicht = { main: 4, volume: 1, oscillator: 1.4, macd: 1.4 };
        try {
            var panes = main.chart.panes();
            panes[0].setStretchFactor(gewicht.main);

            var namen = ['volume', 'oscillator', 'macd'];
            for (var i = 0; i < namen.length; i++) {
                // The live pane, not entry.paneIndex, which goes stale once a pane above it is dropped
                var pane = this._paneOf(this._charts[namen[i]]);
                if (!pane) continue;
                pane.setStretchFactor(gewicht[namen[i]]);
            }

            // The weights alone change nothing: the chart only divides the height again while it
            // lays itself out, and it skips that when the size did not change. One pixel taller and
            // back forces the pass. Without it every panel below the candles gets a height of zero
            // and the chart looks as if the panels were never made - measured on 5.2.1.
            //
            // But a resize keeps the BAR SPACING and moves the visible range to suit, so the nudge
            // walks the chart sideways - and doing that on every refresh is exactly the "it keeps
            // jumping and I keep scrolling back" this rebuild is meant to end. So it only happens
            // when the SET OF PANELS actually changed, which is when someone switches a panel on
            // or off. An ordinary refresh leaves the layout, and the view, entirely alone.
            // Only when a panel actually came or went. A resize keeps the BAR SPACING and moves
            // the visible range to suit, so this nudge walks the chart sideways - and doing that on
            // every refresh is the "it keeps jumping and I keep scrolling back" this rebuild is
            // meant to end. Switching a panel on or off is a moment the user is looking at the
            // layout anyway; a refresh is not.
            if (!veranderd) return;

            var doos = main.container;
            if (doos && doos.clientHeight > 0) {
                main.chart.resize(doos.clientWidth, doos.clientHeight + 1, true);
                main.chart.resize(doos.clientWidth, doos.clientHeight, true);
            }
        }
        catch (e) { }
    },

    _createPane: function () {
        var main = this._charts.main;
        if (!main) return null;

        // The pane comes into being with the first series that names it, so its number is simply
        // the next one after the panes that exist.
        var nummer = 1;
        try { nummer = main.chart.panes().length; } catch (e) { }

        return { chart: main.chart, paneIndex: nummer, series: {} };
    },


    /// Hang the position markers in one sub-panel. Any of its series will do as the anchor — a
    /// primitive draws over the whole pane, it does not belong to the series it is attached to.
    _attachVerticals: function (entry) {
        if (!entry || !this._VerticalPrimitive) return;

        var keys = Object.keys(entry.series || {});
        if (keys.length === 0) return;

        try {
            var primitive = new this._VerticalPrimitive(this._verticals);
            entry.series[keys[0]].attachPrimitive(primitive);
            entry.verticalPrimitive = primitive;
        } catch (e) { }
    },





    _crosshairTime: null,
    _crosshairSyncing: false,


    /// Drop every sub-panel, subscription included.
    ///
    /// The subscription is the point. Each rebuild used to add another handler to the MAIN chart
    /// pointing at a sub-chart that had just been removed, so after a few overlay clicks the main
    /// chart fired a growing stack of handlers on every scroll. Each one takes the _syncing guard,
    /// throws on its dead chart and releases it again — which left the guard held at the moment the
    /// live panel's handler ran, so the panels stopped following, and the main chart could be
    /// dragged off its own candles by a range that belonged to nothing. That is the chart going
    /// black after the third click on an overlay.
    _addPriceLine: function (series, price, color, lineStyle, title, axisLabelVisible) {
        return series.createPriceLine({
            price: price,
            color: color,
            lineWidth: 1,
            lineStyle: lineStyle === undefined ? 2 : lineStyle,
            axisLabelVisible: axisLabelVisible === undefined ? true : axisLabelVisible,
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

        // atrrb, vbs, dbr and bbma used to be listed here. Their lines arrive with a style of their
        // own through setPluginStyles since 17-09-2026 (the plugin declares it, the user's own setting
        // still wins), so a copy here would only be a second place to forget.

        zigzag:         { color: '#ffffff', lineWidth: 1, lineStyle: 0 },
        fibZigzag:      { color: '#ffeb3b', lineWidth: 1, lineStyle: 2 },
    },

    // User-configured styles, keyed the same way as _overlayStyles. Whatever is in here wins over
    // the built-in defaults above, so a series only needs an entry when it was actually changed.
    _userStyles: {},

    setStyles: function (styles) {
        this._userStyles = styles || {};
        this._styleSignature = JSON.stringify(this._userStyles);
    },

    // Part of the shape key of every sub-panel (_verversPanelen). A reused panel only gets new
    // data, never new series options, so a changed colour has to count as a changed shape.
    _styleSignature: '',

    // Styles a plugin overlay asked for, keyed by series key. They sit between the user's own
    // settings and the built-in table: the user still wins, but a plugin line is no longer drawn
    // in the fallback grey just because this file has no entry for it.
    _pluginStyles: {},

    setPluginStyles: function (styles) {
        this._pluginStyles = styles || {};
    },

    // Filled areas between two lines, as supplied by a plugin overlay. Kept until the next setData,
    // which is what paints them.
    _bands: [],
    _bandSeries: [],
    _bandVorm: null,

    setBands: function (bands) {
        // Kept RAW here and shifted in setData, together with everything else. The shift used to
        // happen here, because bands do not travel through setData's four arguments - but setData
        // now decides per interval WHETHER to shift at all, and that decision is not known yet at
        // this point. Doing it in one place also removes the older trap: a band on UTC beside
        // candles on local time is two grids next to each other, which doubles the positions on
        // the time axis and leaves the cloud half a candle beside the candles it belongs to.
        this._bands = bands || [];
    },

    // A custom series that fills between a high and a low per bar, in two colours: one series
    // covers the whole chart and switches colour where the direction changes. Verified against
    // lightweight-charts 4.2 in a standalone page, including zooming and panning.
    //
    // Two things that cost a while to find, so they are written down here:
    //  - a field called "color" on a data point is swallowed by the library; an own name such as
    //    "up" survives into originalData;
    //  - the renderer must honour data.visibleRange. Drawing every bar it is handed puts shapes
    //    across the screen that have nothing to do with what is in view.
    _makeBandSeries: function () {
        var data = null;
        var options = null;

        var renderer = {
            draw: function (target, priceConverter) {
                if (!data || !data.bars || data.bars.length === 0) return;
                // Wrapped for the same reason the zone overlay is: an exception thrown from a
                // series renderer aborts the render pass of the whole pane, and what stays on
                // screen is the previous frame. A cloud that cannot be drawn must cost the cloud,
                // not every line beside it.
                try {
                    // Through the closure variable, not through `this`: the library calls draw()
                    // on whatever it holds, and the binding is not ours to rely on.
                    renderer._drawBand(target, priceConverter);
                } catch (e) {
                    if (window.console) console.error('band draw failed', e);
                }
            },

            _drawBand: function (target, priceConverter) {
                target.useBitmapCoordinateSpace(function (scope) {
                    var ctx = scope.context;
                    var hr = scope.horizontalPixelRatio;
                    var vr = scope.verticalPixelRatio;
                    ctx.save();

                    var from = 0, to = data.bars.length;
                    var visFrom = 0, visTo = data.bars.length;
                    if (data.visibleRange) {
                        visFrom = Math.max(0, data.visibleRange.from);
                        visTo = Math.min(data.bars.length, data.visibleRange.to);
                        from = Math.max(0, visFrom - 1);
                        to = Math.min(data.bars.length, visTo + 1);
                    }

                    // lightweight-charts 5 only brings bar.x up to date for the bars IN VIEW
                    // (visibleRange, "to" exclusive). A bar outside it keeps the x of the last frame
                    // it was visible in - or null if it never was. Measured: after zooming out and
                    // back in, the bar just left of the view still said x = 629 while the first
                    // visible one was at 3. Used as a corner, that stale x is what drew the blocks
                    // with a straight side into the cloud, differently after every zoom.
                    // So an x is only taken from a bar in view. The one bar on either side, which
                    // the cloud needs to run to the edge instead of stopping half a bar short, gets
                    // its x from its visible neighbour and the bar spacing.
                    var spacing = data.barSpacing;
                    var xOf = function (i) {
                        var x = null;
                        if (i >= visFrom && i < visTo)
                            x = data.bars[i].x;
                        else if (i === visFrom - 1 && visFrom < visTo)
                            x = data.bars[visFrom].x - spacing;
                        else if (i === visTo && visTo > visFrom)
                            x = data.bars[visTo - 1].x + spacing;
                        return typeof x === 'number' && isFinite(x) ? x : null;
                    };

                    var run = [];
                    var runColor = null;
                    var flush = function () {
                        if (run.length >= 2 && runColor) {
                            ctx.beginPath();
                            var k;
                            for (k = 0; k < run.length; k++) {
                                if (k === 0) ctx.moveTo(run[k].x, run[k].high);
                                else ctx.lineTo(run[k].x, run[k].high);
                            }
                            for (k = run.length - 1; k >= 0; k--) ctx.lineTo(run[k].x, run[k].low);
                            ctx.closePath();
                            ctx.fillStyle = runColor;
                            ctx.fill();
                        }
                        run = [];
                    };

                    for (var i = from; i < to; i++) {
                        var bar = data.bars[i];
                        var barX = xOf(i);
                        if (!bar || barX === null)
                            continue;
                        // originalData is ABSENT on a whitespace point - the band has no value
                        // there, which is exactly what isWhitespace() reports to the library. Reading
                        // .high off it threw, and an exception here aborts the whole render pass, so
                        // the pane kept the pixels of the previous frame: shapes that no longer line
                        // up with the candles under them, until something forced a full redraw.
                        var d = bar.originalData;
                        if (!d || d.high === null || d.high === undefined || d.low === null || d.low === undefined) {
                            flush();
                            runColor = null;
                            continue;
                        }
                        var point = {
                            x: barX * hr,
                            high: priceConverter(d.high) * vr,
                            low: priceConverter(d.low) * vr,
                        };
                        var wanted = d.up ? options.fillUp : options.fillDown;
                        if (wanted !== runColor) {
                            // The turning candle belongs to both runs, or a seam opens up.
                            if (run.length) {
                                run.push(point);
                                flush();
                            }
                            runColor = wanted;
                        }
                        run.push(point);
                    }
                    flush();
                    ctx.restore();
                });
            },
        };

        return {
            priceValueBuilder: function (d) { return [d.low, d.high]; },
            isWhitespace: function (d) { return d.high === undefined || d.high === null; },
            defaultOptions: function () {
                return {
                    fillUp: 'rgba(128,128,128,0.15)',
                    fillDown: 'rgba(128,128,128,0.15)',
                    lastValueVisible: false,
                    priceLineVisible: false,
                };
            },
            update: function (d, o) { data = d; options = o; },
            renderer: function () { return renderer; },
        };
    },
    _styleFor: function (key) {
        var user = this._userStyles[key];
        if (user)
            return user;
        var plugin = this._pluginStyles[key];
        if (plugin && plugin.color)
            return plugin;
        return this._overlayStyles[key] || { color: '#888', lineWidth: 1, lineStyle: 0 };
    },

    // The same colour at another transparency. A style entry holds one colour per series, while a
    // filled area needs both a translucent body and a firmer outline, so the outline is derived
    // from the configured colour here instead of adding a second entry to the settings screen.
    _withAlpha: function (color, alpha) {
        if (!color)
            return color;

        var rgb = color.match(/^rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)/i);
        if (rgb)
            return 'rgba(' + rgb[1] + ',' + rgb[2] + ',' + rgb[3] + ',' + alpha + ')';

        var hex = color.replace('#', '');
        if (hex.length === 3)
            hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
        if (hex.length !== 6 && hex.length !== 8)
            return color;

        return 'rgba(' + parseInt(hex.substr(0, 2), 16) + ',' +
            parseInt(hex.substr(2, 2), 16) + ',' +
            parseInt(hex.substr(4, 2), 16) + ',' + alpha + ')';
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

        // TERUGGEDRAAID 28-09-2026. Hier stond een poging om een candle van een DAG of langer niet
        // naar lokale tijd te schuiven en de klok van de tijdas te halen, omdat zo'n candle voor een
        // kalenderdag staat en niet voor een moment - op een weekchart zat elke candle op maandag
        // 02:00 en dat stond ook op de as. Die wijziging maakte de as slechter in plaats van beter
        // (er bleef vrijwel geen enkel label over), en ik kon het niet nameten voordat het bij de
        // gebruiker stond. Eerst een manier om dit te ZIEN, dan pas opnieuw proberen.
        this._localizeTimes(candles);
        this._localizeTimes(overlays);
        this._localizeTimes(panels);
        this._localizeTimes(extras);
        this._localizeTimes(this._bands);

        var self = this;
        var mainEntry = this._charts.main;

        // While this runs, the sub-charts are destroyed and rebuilt. A fresh sub-chart fits itself
        // to its own data and reports that through subscribeVisibleLogicalRangeChange, and the
        // two-way sync then pushes that range onto the main chart. Those callbacks arrive after
        // setData has returned, so the plain _syncing flag never caught them.
        //
        // Numbered, because the guard is released from a setTimeout and two runs can overlap:
        // clicking overlays quickly queues one setData behind another, and the older run's timeout
        // would then clear the flag while the newer run is still rebuilding its panels — letting
        // through exactly the callbacks this is here to block. Only the newest run releases it.
        this._settingData = true;
        var setDataRun = ++this._setDataRun;

        // The new candles. Where the user was looking is NOT remembered and put back - the chart
        // keeps its own view across a setData, and reading it out to write it back was what made
        // it jump. See the note further down, where that write-back used to be.
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

        // The filled bands of a plugin overlay - the cloud. KEPT between refreshes, like the
        // panels: a series that is thrown away and made again is a series that has no pixels for
        // a frame, and what stays on screen in that frame is the one before it. That is the cloud
        // leaving pieces of itself behind and then quietly repairing them.
        //
        // Only when the SET of bands changes - another plugin, another colour - is it built anew.
        var bandVorm = (this._bands || []).map(function (b) {
            return (b && b.key) + ':' + (b && b.fillUp) + ':' + (b && b.fillDown);
        }).join(',');

        if (bandVorm !== this._bandVorm) {
            this._bandVorm = bandVorm;
            this._bandSeries.forEach(function (s) {
                try { mainEntry.chart.removeSeries(s); } catch (e) { }
            });
            this._bandSeries = [];

            if (this._bands && this._bands.length > 0 && mainEntry.chart.addCustomSeries) {
                var bandSelf = this;
                this._bands.forEach(function (band) {
                    if (!band || !band.points || band.points.length === 0) return;
                    try {
                        var series = mainEntry.chart.addCustomSeries(bandSelf._makeBandSeries(), {
                            fillUp: band.fillUp,
                            fillDown: band.fillDown,
                            priceLineVisible: false,
                            lastValueVisible: false,
                            crosshairMarkerVisible: false,
                            // Kept out of the autoscale: the cloud lies between lines that already
                            // count, so letting it vote only risks pulling the price scale around on
                            // every redraw.
                            autoscaleInfoProvider: function () { return null; },
                        });
                        series.__bandKey = band.key;
                        // Under the candles and the lines. On the first build it is made before
                        // them and lands there by itself, but a new colour rebuilds it on a chart
                        // that already has them, and a new series goes on top of everything.
                        try { series.setSeriesOrder(bandSelf._bandSeries.length); } catch (e) { }
                        bandSelf._bandSeries.push(series);
                    } catch (e) {
                        // A band that cannot be drawn must never take the whole chart down with it.
                        console.warn('band ' + band.key + ' failed: ' + e);
                    }
                });
            }
        }

        // The data goes in every time. BY KEY, not by position: a band with no points is skipped
        // when the series are made, so the two lists do not line up one to one.
        var perSleutel = {};
        (this._bands || []).forEach(function (band) {
            if (band && band.key) perSleutel[band.key] = band;
        });
        this._bandSeries.forEach(function (serie) {
            var band = perSleutel[serie.__bandKey];
            if (!band || !band.points) return;
            try {
                serie.setData(band.points.filter(function (pt) { return inRange(pt.time); }));
            } catch (e) {
                // Rejected data (a time twice, out of order) silently left the cloud away
                console.warn('band ' + serie.__bandKey + ' setData failed: ' + e);
            }
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

                var series = mainEntry.chart.addSeries(LightweightCharts.LineSeries, {
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
        // One handle, kept and reused. createSeriesMarkers ADDS a layer where the old setMarkers
        // replaced the list, so calling it per refresh stacks a new set of markers on the candles
        // every single redraw.
        try {
            if (this._markerLaag && this._markerSerie === mainEntry.series.candles)
                this._markerLaag.setMarkers(markers);
            else {
                this._markerLaag = LightweightCharts.createSeriesMarkers(mainEntry.series.candles, markers);
                this._markerSerie = mainEntry.series.candles;
            }
        } catch (e) { }

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
                    mainEntry.series.candles, pl.price, pl.color, pl.lineStyle, pl.title,
                    pl.axisLabelVisible);
                mainEntry.priceLines.push(line);
            });
        }

        // Set BEFORE the panels are touched: a panel that IS built picks these up as it is made
        this._verticals = (extras && extras.verticals) ? extras.verticals : [];

        // Panels: kept where they can be, rebuilt only where they must. The layout is only
        // disturbed when one actually came or went.
        var veranderd = this._verversPanelen(panels);
        this._verdeelPanelen(veranderd);

        // Another coin over the same days: the price axis goes back to automatic, the time window
        // stays. Unless the window does not overlap what this coin HAS - a coin listed last month
        // shown on a window from last year is a blank pane - and then it falls back to fitting.
        if (this._pendingPriceOnly && !this._pendingFit) {
            this._pendingPriceOnly = false;
            try { mainEntry.chart.priceScale('right').applyOptions({ autoScale: true }); } catch (e) { }

            var zichtbaar = null;
            try { zichtbaar = mainEntry.chart.timeScale().getVisibleRange(); } catch (e) { }
            var eerste = candles && candles.length ? candles[0].time : null;
            var laatste = candles && candles.length ? candles[candles.length - 1].time : null;
            var overlap = zichtbaar && eerste !== null
                && zichtbaar.to >= eerste && zichtbaar.from <= laatste;
            if (!overlap)
                this._zoomLast(candles ? candles.length : 0);
        }

        // Only reset the view when the chart shows something else than before. Fitting on every
        // call threw away the zoom and scroll position on each overlay toggle.
        if (this._pendingFit) {
            this._pendingFit = false;

            // The PRICE axis has to come back too, not just the time axis. Dragging that axis turns
            // autoScale off and the manual range then sticks - across a symbol change as well. A
            // coin at 3.46 shown on a scale still set to 84..91 draws its candles far below the
            // pane: an empty chart, with nothing on screen saying why.
            try { mainEntry.chart.priceScale('right').applyOptions({ autoScale: true }); } catch (e) { }

            // Opened from a position: the interesting stretch sits in the MIDDLE of the series,
            // with margin candles drawn on both sides, so zooming to the last N would land past it.
            if (this._pendingWindow) {
                var w = this._pendingWindow;
                this._pendingWindow = null;
                try { mainEntry.chart.timeScale().setVisibleRange({ from: w.time1, to: w.time2 }); }
                catch (e) { this._zoomLast(candles ? candles.length : 0); }
            }
            else {
                this._zoomLast(candles ? candles.length : 0);
            }
        }
        // Nothing else. The view is NOT written back any more.
        //
        // It had to be, as long as every refresh threw the sub-charts away and built new ones that
        // each fitted themselves to their own data. With one chart there is nothing to rebuild:
        // setData on a series leaves the time scale exactly where it stood - measured on 5.2.1.
        //
        // And writing it back was not free. The range was read at the START of this function, so
        // anything that had not settled yet was captured stale and then forced back onto the
        // chart: the view jumping away from where it was put, which is the thing that made this
        // chart tiring to use.

        // Release the sync guard only once the sub-charts have settled. Their range callbacks are
        // queued, not immediate, so clearing it here and now would let them through after all.
        setTimeout(function () {
            // A newer setData took over; it owns the guard and will release it itself.
            if (self._setDataRun !== setDataRun) return;

            self._settingData = false;

            // After the pane has laid out, so priceToCoordinate answers on the scale actually in
            // use. An overlay toggle does not go through the reset above, which is how ticking a
            // band on and off could leave the candles off screen for good. Time axis first: a
            // window with no bars in it makes the price question meaningless.
            self._ensureCandlesInView(candles);
            self._ensureCandlesVisible(candles);

        }, 0);
    },

    _setDataRun: 0,


    /// Safety net for a time scale scrolled clear of the candles. A price scale off its range still
    /// leaves an axis to read; a time scale off its range leaves nothing at all — no candles, no
    /// price axis, an empty pane — which is what a rebuilt sub-panel pushing its own range onto the
    /// main chart used to cause. Snaps back to the last candles only when not a single bar is in
    /// view, so a deliberate scroll into the empty space on the right is left alone.
    _ensureCandlesInView: function (candles) {
        var mainEntry = this._charts.main;
        if (!mainEntry || !candles || candles.length === 0) return;

        try {
            // In TIME, not in bar positions. A position on the axis is only the same thing as an
            // index into the candles while every position IS a candle - and a series whose points
            // sit on times of their own (a band that was not put on the same clock) inserts
            // positions between them. The count then no longer matches, every view of the recent
            // candles reads as "past the end", and this net snaps the chart back on every refresh:
            // the very jump it exists to prevent. Times cannot drift apart that way.
            var visible = mainEntry.chart.timeScale().getVisibleRange();
            if (visible) {
                var first = candles[0].time;
                var last = candles[candles.length - 1].time;
                if (visible.to >= first && visible.from <= last)
                    return;
            }

            // No range at all means the window holds no bars whatsoever, which is exactly the case
            // this is here for.
            this._zoomLast(candles.length);
        } catch (e) { }
    },

    /// Safety net for a price scale that no longer covers the candles at all.
    ///
    /// autoScale is deliberately switched off by lightweight-charts as soon as the price axis is
    /// dragged, and that is worth keeping - a manual range is a choice. What is not a choice is
    /// that range surviving a switch to a coin two orders of magnitude away, or an overlay whose
    /// values pushed the scale somewhere the candles are not. The chart then looks broken with no
    /// way back except a double click on an axis nobody knows is clickable.
    ///
    /// Only fires when EVERY candle is outside the pane. A deliberate zoom always keeps some of
    /// them in view, so a real manual range is never taken away.
    _ensureCandlesVisible: function (candles) {
        var mainEntry = this._charts.main;
        if (!mainEntry || !candles || candles.length === 0) return;

        var height = mainEntry.container.clientHeight;
        if (!height) return;

        var high = -Infinity, low = Infinity;
        for (var i = 0; i < candles.length; i++) {
            if (candles[i].high > high) high = candles[i].high;
            if (candles[i].low < low) low = candles[i].low;
        }
        if (!isFinite(high) || !isFinite(low)) return;

        try {
            var yHigh = mainEntry.series.candles.priceToCoordinate(high);
            var yLow = mainEntry.series.candles.priceToCoordinate(low);
            if (yHigh === null || yLow === null) return;

            // Both ends above the top edge, or both below the bottom edge: nothing is on screen.
            if ((yHigh < 0 && yLow < 0) || (yHigh > height && yLow > height))
                mainEntry.chart.priceScale('right').applyOptions({ autoScale: true });
        } catch (e) { }
    },

    // How many candles the initial view shows. fitContent squeezed the whole loaded history into
    // the pane, which left the candles as hair-thin slivers; the Avalonia chart zooms to the last
    // ZonesDlz.CandleCountZoom candles instead.
    _zoomCandles: 125,

    setZoomCandles: function (count) {
        if (typeof count === 'number' && count > 0)
            this._zoomCandles = count;
    },

    /// Land the next reset-view on an explicit time window instead of on the last candles. Pass
    /// {time1, time2} in the same UTC seconds the candles use — _localizeTimes shifts those two
    /// keys along with everything else, so the window and the candles stay on one clock. Cleared
    /// by the setData that consumes it; call it again for the next window.
    zoomToWindow: function (window) {
        if (!window) {
            this._pendingWindow = null;
            return;
        }
        this._pendingWindow = this._localizeTimes({ time1: window.time1, time2: window.time2 });
    },

    _pendingWindow: null,

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

    /// Another coin, same stretch of market. Keeps the time window the user is looking at and only
    /// puts the PRICE axis back on automatic - a coin at 3,46 on a scale still set to 84..91 draws
    /// its candles far below the pane, which reads as an empty chart with nothing saying why.
    ///
    /// Zooming to the last candles on every symbol change threw away where he was looking, and
    /// comparing two coins over the same days then meant scrolling back by hand every time.
    keepTimeWindow: function () {
        this._pendingPriceOnly = true;
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

    /// Bring the panels into line with what was asked for, keeping what can be kept.
    ///
    /// They used to be thrown away and built again on every refresh, because they were charts of
    /// their own and there was no cheaper way. That is what made the chart restless: new panels
    /// have no size until the chart lays itself out again, that pass moves the view sideways, and
    /// the view then had to be written back from a value read before any of it happened.
    ///
    /// Now a panel is only rebuilt when its SHAPE changes - when the stochastic is switched on
    /// beside the RSI, say, so the pane needs other series. An ordinary refresh replaces the data
    /// in the series that are already there and touches nothing else.
    ///
    /// Returns whether any panel was made or dropped, which is the only case the layout has to be
    /// worked out again.
    _verversPanelen: function (panels) {
        var styleKey = '|' + this._styleSignature;
        var wil = {
            volume: panels && panels.volume ? 'volume' + styleKey : null,
            oscillator: panels && (panels.rsi || panels.stoch || panels.lux)
                ? [panels.rsi ? 'rsi:' + panels.rsi.oversold + '-' + panels.rsi.overbought : '',
                   panels.stoch ? 'stoch:' + panels.stoch.oversold + '-' + panels.stoch.overbought : '',
                   panels.lux ? 'lux' : ''].join('|') + styleKey
                : null,
            macd: panels && panels.macd ? 'macd' + styleKey : null,
        };

        var veranderd = false;
        var self = this;

        // The panes always stand in this order under the candles. A new pane can only be added
        // at the BOTTOM, so a panel that changes takes every panel below it along: they are all
        // dropped and made again in order. Dropping only the changed one put its replacement
        // under the macd (lux, then macd, then rsi gave candles - macd - oscillator).
        var volgorde = ['volume', 'oscillator', 'macd'];
        var eersteWijziging = volgorde.length;
        for (var v = 0; v < volgorde.length; v++) {
            var bestaand = this._charts[volgorde[v]];
            var huidig = bestaand ? bestaand.vorm : null;
            if (huidig !== wil[volgorde[v]]) {
                eersteWijziging = v;
                break;
            }
        }

        // Back to front, so the panes above the one being dropped keep their place
        for (var w = volgorde.length - 1; w >= eersteWijziging; w--) {
            if (this._charts[volgorde[w]]) {
                this._verwijderPaneel(volgorde[w]);
                veranderd = true;
            }
        }

        if (panels) {
            if (wil.volume) {
                if (this._charts.volume) this._vulVolume(this._charts.volume, panels.volume);
                else { this._createVolumePanel(panels.volume); veranderd = true; }
                if (this._charts.volume) this._charts.volume.vorm = wil.volume;
            }
            // RSI, stochastic and Lux share one panel — they are all bounded oscillators,
            // so a single pane saves a lot of vertical space
            if (wil.oscillator !== null) {
                if (this._charts.oscillator)
                    this._vulOscillator(this._charts.oscillator, panels.rsi, panels.stoch, panels.lux);
                else {
                    this._createOscillatorPanel(panels.rsi, panels.stoch, panels.lux);
                    veranderd = true;
                }
                if (this._charts.oscillator) this._charts.oscillator.vorm = wil.oscillator;
            }
            if (wil.macd) {
                if (this._charts.macd) this._vulMacd(this._charts.macd, panels.macd);
                else { this._createMacdPanel(panels.macd); veranderd = true; }
                if (this._charts.macd) this._charts.macd.vorm = wil.macd;
            }
        }

        return veranderd;
    },

    /// Drop one panel: its series first, then the pane.
    _verwijderPaneel: function (naam) {
        var entry = this._charts[naam];
        var main = this._charts.main;
        if (!entry || !main) return;

        // The pane is looked up through its own series, not through entry.paneIndex: that is the
        // number the pane had when it was made, and dropping a pane above it renumbers it.
        var pane = this._paneOf(entry);

        Object.keys(entry.series || {}).forEach(function (sleutel) {
            try { main.chart.removeSeries(entry.series[sleutel]); } catch (e) { }
        });

        // lightweight-charts 5 removes a pane by itself as soon as its last series is gone. The
        // removePane(entry.paneIndex) that stood here then removed the pane that had moved up into
        // that number - the macd, when the oscillator above it was rebuilt. Only a pane that is
        // really still there and really empty is removed here.
        try {
            if (pane && main.chart.panes().indexOf(pane) > 0 && pane.getSeries().length === 0)
                main.chart.removePane(pane.paneIndex());
        } catch (e) { }
        delete this._charts[naam];
    },

    /// The pane a panel lives in, as it stands NOW. Null when the panel has no series.
    _paneOf: function (entry) {
        if (!entry || !entry.series) return null;
        var keys = Object.keys(entry.series);
        if (keys.length === 0) return null;
        try { return entry.series[keys[0]].getPane(); } catch (e) { return null; }
    },

    _vulVolume: function (entry, candles) {
        var up = this._styleFor('volumeUp').color;
        var down = this._styleFor('volumeDown').color;
        try {
            entry.series.volume.setData(candles.map(function (c) {
                return { time: c.time, value: c.volume || 0,
                         color: c.close >= c.open ? up : down };
            }));
        } catch (e) { }
    },

    _vulOscillator: function (entry, rsiData, stochData, luxData) {
        try {
            if (rsiData && entry.series.rsi) entry.series.rsi.setData(rsiData.data);
            if (stochData && entry.series.k) entry.series.k.setData(stochData.k);
            if (stochData && entry.series.d) entry.series.d.setData(stochData.d);
            if (luxData && entry.series.luxOversold) {
                // Same transform as _createOscillatorPanel: oversold hangs down from 100. Passing
                // the raw reading here filled the whole pane red after the first refresh.
                entry.series.luxOversold.setData(luxData.map(function (d) {
                    return { time: d.time, value: 100 - d.oversold };
                }));
                entry.series.luxOverbought.setData(luxData.map(function (d) {
                    return { time: d.time, value: d.overbought };
                }));
            }
        } catch (e) { }
    },

    _vulMacd: function (entry, macdData) {
        try {
            entry.series.histogram.setData(macdData.histogram);
            entry.series.macd.setData(macdData.macdLine);
            entry.series.signal.setData(macdData.signal);
        } catch (e) { }
    },

    _createVolumePanel: function (candles) {
        var entry = this._createPane();
        if (!entry) return;

        var up = this._styleFor('volumeUp').color;
        var down = this._styleFor('volumeDown').color;

        var series = entry.chart.addSeries(LightweightCharts.HistogramSeries, {
            priceFormat: { type: 'volume' },
        }, entry.paneIndex);
        series.setData(candles.map(function (c) {
            return {
                time: c.time,
                value: c.volume || 0,
                color: c.close >= c.open ? up : down,
            };
        }));
        entry.series.volume = series;
        this._attachVerticals(entry);
        this._charts.volume = entry;
    },

    _createOscillatorPanel: function (rsiData, stochData, luxData) {
        var entry = this._createPane();
        if (!entry) return;

        var levelSeries = null;

        if (rsiData) {
            var rsiStyle = this._styleFor('rsi');
            var rsiSeries = entry.chart.addSeries(LightweightCharts.LineSeries, {
                color: rsiStyle.color, lineWidth: rsiStyle.lineWidth, lineStyle: rsiStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, crosshairMarkerVisible: false, title: 'RSI',
            }, entry.paneIndex);
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
            var kSeries = entry.chart.addSeries(LightweightCharts.LineSeries, {
                color: kStyle.color, lineWidth: kStyle.lineWidth, lineStyle: kStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, crosshairMarkerVisible: false, title: '%K',
            }, entry.paneIndex);
            kSeries.setData(stochData.k);

            var dStyle = this._styleFor('stochD');
            var dSeries = entry.chart.addSeries(LightweightCharts.LineSeries, {
                color: dStyle.color, lineWidth: dStyle.lineWidth, lineStyle: dStyle.lineStyle,
                lastValueVisible: true, priceLineVisible: false, crosshairMarkerVisible: false, title: '%D',
            }, entry.paneIndex);
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
            // Drawn the way the Avalonia chart draws it: both readings are PERCENTAGES of the
            // multi-length RSI bucket, so 0..100, on the same scale the RSI and the stochastic use.
            // Overbought grows up from 0, oversold hangs down from 100.
            //
            // What was here before assumed "Lux counts are unbounded" and gave it a private,
            // auto-scaling axis squeezed into the bottom quarter of the pane, with overbought
            // mirrored below zero. The values are bounded and always were - LuxIndicator.CalculateRange
            // returns 100 * count / N - so that axis stretched whatever happened to be in view to fill
            // its quarter, and the same reading looked different from one screen to the next.
            var pinTo0to100 = function () {
                return { priceRange: { minValue: 0, maxValue: 100 } };
            };

            // Filled areas, not bars, the same shape the Avalonia chart draws with its OxyPlot
            // AreaSeries: an outline at the reading with a translucent body running back to the
            // level it is measured from. A baseline series is used rather than an area series
            // because its fill stops at an exact price (0 and 100) instead of at the edge of the
            // pane, so both areas meet the reference levels the readings are defined against.
            var osStyle = this._styleFor('luxOversold');
            var obStyle = this._styleFor('luxOverbought');
            var transparent = 'rgba(0,0,0,0)';

            // Oversold: filled from the top (100) down to the reading, exactly Pine's per_under.
            var osSeries = entry.chart.addSeries(LightweightCharts.BaselineSeries, {
                baseValue: { type: 'price', price: 100 },
                bottomLineColor: this._withAlpha(osStyle.color, 0.9),
                bottomFillColor1: osStyle.color,
                bottomFillColor2: osStyle.color,
                topLineColor: transparent,
                topFillColor1: transparent,
                topFillColor2: transparent,
                lineWidth: osStyle.lineWidth || 1,
                lineStyle: osStyle.lineStyle || 0,
                lastValueVisible: false, priceLineVisible: false, crosshairMarkerVisible: false,
                autoscaleInfoProvider: pinTo0to100,
            }, entry.paneIndex);
            osSeries.setData(luxData.map(function (d) {
                return { time: d.time, value: 100 - d.oversold };
            }));

            // Overbought: filled from the baseline (0) up to the reading.
            var obSeries = entry.chart.addSeries(LightweightCharts.BaselineSeries, {
                baseValue: { type: 'price', price: 0 },
                topLineColor: this._withAlpha(obStyle.color, 0.9),
                topFillColor1: obStyle.color,
                topFillColor2: obStyle.color,
                bottomLineColor: transparent,
                bottomFillColor1: transparent,
                bottomFillColor2: transparent,
                lineWidth: obStyle.lineWidth || 1,
                lineStyle: obStyle.lineStyle || 0,
                lastValueVisible: false, priceLineVisible: false, crosshairMarkerVisible: false,
                autoscaleInfoProvider: pinTo0to100,
            }, entry.paneIndex);
            obSeries.setData(luxData.map(function (d) {
                return { time: d.time, value: d.overbought };
            }));

            entry.series.luxOversold = osSeries;
            entry.series.luxOverbought = obSeries;

            // The levels the two readings are measured against, the same three dashed lines the
            // Avalonia chart draws: 0 (overbought grows from here), 100 (oversold hangs from here)
            // and the Pine mid-line at 50. A pane showing only Lux has nothing else to read the
            // fills against. No axis labels - the price scale already prints those values, and the
            // RSI and stochastic thresholds are the ones worth naming there. The colour is the one
            // the MACD zero line already uses, so both sub-panels mark their reference levels the
            // same way; OxyPlot draws these at grey alpha 60/255, which comes out the same.
            var luxLevel = 'rgba(150,150,150,0.3)';
            this._addPriceLine(obSeries, 0, luxLevel, 2, '', false);
            this._addPriceLine(obSeries, 50, luxLevel, 2, '', false);
            this._addPriceLine(obSeries, 100, luxLevel, 2, '', false);
        }

        this._attachVerticals(entry);
        this._charts.oscillator = entry;
    },

    _createMacdPanel: function (macdData) {
        var entry = this._createPane();
        if (!entry) return;

        var histSeries = entry.chart.addSeries(LightweightCharts.HistogramSeries, {
            lastValueVisible: false, priceLineVisible: false,
        }, entry.paneIndex);
        histSeries.setData(macdData.histogram);

        var macdStyle = this._styleFor('macdLine');
        var macdSeries = entry.chart.addSeries(LightweightCharts.LineSeries, {
            color: macdStyle.color, lineWidth: macdStyle.lineWidth, lineStyle: macdStyle.lineStyle,
            lastValueVisible: true, priceLineVisible: false, crosshairMarkerVisible: false, title: 'MACD',
        }, entry.paneIndex);
        macdSeries.setData(macdData.macdLine);

        var signalStyle = this._styleFor('macdSignal');
        var signalSeries = entry.chart.addSeries(LightweightCharts.LineSeries, {
            color: signalStyle.color, lineWidth: signalStyle.lineWidth, lineStyle: signalStyle.lineStyle,
            lastValueVisible: true, priceLineVisible: false, crosshairMarkerVisible: false, title: 'Signal',
        }, entry.paneIndex);
        signalSeries.setData(macdData.signal);

        this._addPriceLine(macdSeries, 0, 'rgba(150,150,150,0.3)');

        entry.series.histogram = histSeries;
        entry.series.macd = macdSeries;
        entry.series.signal = signalSeries;
        this._attachVerticals(entry);
        this._charts.macd = entry;
    },

    addMarkers: function (markers) {
        if (!this._loaded || !this._charts.main) return;
        var serie = this._charts.main.series.candles;
        if (this._markerLaag && this._markerSerie === serie)
            this._markerLaag.setMarkers(markers);
        else {
            this._markerLaag = LightweightCharts.createSeriesMarkers(serie, markers);
            this._markerSerie = serie;
        }
    },

    dispose: function () {
        // One chart, so one remove. Every panel entry points at the SAME chart object now, and
        // removing it a second time throws.
        try { if (this._charts.main) this._charts.main.chart.remove(); } catch (e) { }
        this._charts = {};
        this._markerLaag = null;
        this._markerSerie = null;
        this._zonePrimitive = null;
        this._segmentPrimitive = null;
        this._measurePrimitive = null;
        // The cloud series belonged to the chart just removed. Kept, the next setData saw an
        // unchanged _bandVorm, made no new ones and only filled the dead ones - so after leaving
        // the Chart tab and coming back (or a theme change) the cloud was gone for good.
        this._bandSeries = [];
        this._bandVorm = null;
        this._lastCandles = null;
        this._loaded = false;
    }
};

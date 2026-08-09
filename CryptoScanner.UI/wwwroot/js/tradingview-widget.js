window.TradingViewWidget = {
    load: function (symbol) {
        var container = document.getElementById('tradingview-widget-container');
        if (!container) return;

        container.innerHTML = '';

        var widget = document.createElement('div');
        widget.id = 'tradingview_chart';
        widget.style.width = '100%';
        widget.style.height = '100%';
        container.appendChild(widget);

        var script = document.createElement('script');
        script.type = 'text/javascript';
        script.src = 'https://s3.tradingview.com/tv.js';
        script.onload = function () {
            if (typeof TradingView !== 'undefined') {
                new TradingView.widget({
                    container_id: 'tradingview_chart',
                    autosize: true,
                    symbol: symbol,
                    interval: '60',
                    timezone: 'Etc/UTC',
                    theme: 'dark',
                    style: '1',
                    locale: 'en',
                    toolbar_bg: '#1a1a1a',
                    enable_publishing: false,
                    hide_side_toolbar: false,
                    allow_symbol_change: true,
                    save_image: false,
                });
            }
        };
        container.appendChild(script);
    }
};

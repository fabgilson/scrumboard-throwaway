function chartSetupById(chartElemId, config) {
    let chartElem = document.getElementById(chartElemId);
    if(!chartElem) return;
    chartSetup(chartElem, config);
}

function chartSetup(chartElem, config) {
    let ctx = chartElem.getContext('2d');
    let chart = ctx.chart;
    if (chart === undefined) {
        ctx.chart = new Chart(ctx, config);
    } else {
        chart.data = config.data;
        Object.assign(chart.options, config.options);
        chart.update();
    }
}

function useExternalTooltip(chartElem, dotnet)
{
    let ctx = chartElem.getContext('2d');
    let chart = ctx.chart;
    chart.config.options.plugins.tooltip.external = async (context) => {
        const { chart, tooltip } = context;

        if (tooltip.opacity === 0) {
            await dotnet.invokeMethodAsync('hideTooltip');
            return;
        }

        const {offsetLeft: positionX, offsetTop: positionY} = chart.canvas;

        let x = positionX + tooltip.caretX;
        let y = positionY + tooltip.caretY;

        await dotnet.invokeMethodAsync('showTooltip', tooltip.dataPoints.map(point => point.raw), x, y);
    };
    chart.update();
}
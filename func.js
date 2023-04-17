function showWeek() {
    var str = "";
    const start = moment().weekday(1).format('MM-DD'); //本週一
    const end = moment().weekday(7).format('MM-DD'); //本週日
    str += moment().format('YYYY')+"W"+moment().isoWeek();
    str += " ("+start + "~" + end+")";
    str += ", 今天日期: " + moment().format('MM-DD');
    $('#subTitle').text(str);
}

function setWeek(startW, endW) {
    var str = "";
    str += "當前查詢週次: "+moment(startW).format('YYYY') + "第" + moment(startW).format('W')+ "週" + "~" + moment(endW).format('YYYY') + "第" + moment(endW).format('W')+ "週";
    $('#alertWeek').text(str);
}

function showDBUpdate() {
    var str = "資料最後更新時間: "+moment(DB_UpdateTime).format('YYYY-MM-DD')+"(凌晨)";
    $('#DBUpdate').text(str);
}

function getXaxis(dateType, startDateStr, enDateStr) {
    var startDate = moment(startDateStr).toDate();
    var enDate = moment(enDateStr).toDate();
    //console.log("[getXaxis]", startDate.getDate(),  enDate.getDate())
    if(dateType == "week") {
        var startW= moment(startDate).format('yyyy-W');
        var endW = moment(enDate).format('yyyy-W'); //本週日
        return paddingWeek(startW, endW);
    } else {
        if(startDate.getMonth() != enDate.getMonth()) {
            return paddingDate(startDate).concat(paddingDate(enDate, "reverse"));
        } else {
            return paddingTwoDate(startDate, enDate);
        }
    }
}

function paddingWeek(startW, endW) {
    var stAry = startW.split("-"); // 2022-33 年-週數
    var lastDate = moment(stAry[0]+"-12-31").format('yyyy-W');
    var lastAry = lastDate.split("-");
    var enAry = endW.split("-"); // 2022-35
    var Xaxis = [];
    if(parseInt(stAry[0]) < parseInt(enAry[0])) {
        for(var i = parseInt(stAry[1]); i <= parseInt(lastAry[1]); i++) {
            Xaxis.push(stAry[0]+"-"+i);
        }
        for(var i = 1; i <= parseInt(enAry[1]); i++) {
            Xaxis.push(enAry[0]+"-"+i);
        }
    } else {
        for(var i = parseInt(stAry[1]); i <= parseInt(enAry[1]); i++) {
            Xaxis.push(stAry[0]+"-"+i);
        }
    }
    return Xaxis;
}

function paddingDate(dt, direction) {
    var Xaxis = [];
    var month = dt.getMonth(); // 0~11
    var day = dt.getDate();
    var year = dt.getFullYear();
    var daysInMonth = new Date(year, month + 1, 0).getDate();

    //positive
    var startInx = day;
    var endInx = daysInMonth;
    if(direction == "reverse") {
        startInx = 1;
        endInx = day;
    }

    for(var i = startInx; i <= endInx; i++)
        Xaxis.push(year+"-"+numberPadZero(month+1)+"-"+numberPadZero(i));

    return Xaxis;
}

function paddingTwoDate(dt1, dt2) {
    var Xaxis = [];
    var day = dt1.getDate();
    var month = dt1.getMonth(); // 0~11
    var year = dt1.getFullYear();
    var day2 = dt2.getDate();

    for(var i = day; i <= day2; i++)
        Xaxis.push(year+"-"+numberPadZero(month+1)+"-"+numberPadZero(i));

    return Xaxis;
}

function numberPadZero(num) {
    return num.toString().padStart(2,"0");
}

function layoutSelectItems(items, divID) {
    var select = document.getElementById(divID);
    if(Array.isArray(items)) {
        items.forEach(function(element){
            var option = document.createElement("option");
            option.text = element;
            option.value = element;
            select.appendChild(option);
        });
    } else {
        Object.keys(items).forEach(function(code){
            var option = document.createElement("option");
            option.text = items[code];
            option.value = code;
            select.appendChild(option);
        });
    }
}

function layoutCheckItems(items) {
    items.forEach(function(element){
        $("#yearList").append('<li class="list-group-item"> ' +
        '<input class="form-check-input me-1" type="checkbox" name="listGroupCheckbox value="'+element+'" id="checkbox'+element+'" checked> '+
        '<label class="form-check-label" for="checkbox'+element+'">'+element+'</label>' +
        '</li>');
    });
}

function customRange(input) {
    if (input.id == 'datepickerEnd') {
        var minDate = new Date($('#datepickerStart').val());
        minDate.setDate(minDate.getDate() + 1)
        return {
            minDate: minDate
        };
    }
    return {}
}

function getProductName(labelDProducts, str) {
    return (labelDProducts[str] !== null)? labelDProducts[str] : ""
}

function changeChartType(tctx, tmyChart, chartType, _tconfig) {
    var temp = jQuery.extend(true, {}, _tconfig);
    temp.type = chartType;
    if (tmyChart) {    tmyChart.destroy();  }
    return new Chart(tctx, temp);
}

function changeDateFormatType(tctx, tmyChart, search_dateformat, _tconfig) {
    var temp = jQuery.extend(true, {}, _tconfig);
    if(search_dateformat == "week") {
        temp.options.scales.x = {
            title: {
                display: true,
                text: '日期'
            }
        };
    } else {
        temp.options.scales.x = {
            type: 'time',
            title: {
                display: true,
                text: '日期'
            }
        };
    }

    if (tmyChart) {    tmyChart.destroy();  }
    return new Chart(tctx, temp);
}


function refleshChart(myChart, chartType, chartdatas) {
    myChart.data.labels = chartdatas.x_axis;
    myChart.data.datasets = chartdatas.datasets;
    myChart.update();
}

const CHART_COLORS = {
    red: 'rgb(255, 99, 132)',
    orange: 'rgb(255, 159, 64)',
    yellow: 'rgb(255, 205, 86)',
    green: 'rgb(75, 192, 192)',
    blue: 'rgb(54, 162, 235)',
    purple: 'rgb(153, 102, 255)',
    grey: 'rgb(201, 203, 207)'
};

function reBuildChartData(chartData, search_country, yearSelectList) {
    const currentYear = new Date().getFullYear();
    var datasets = [];
    yearSelectList.forEach(function(y){
        var randomColor = getRandomColor(['#000000', '#FFFFFF']); // 排除黑色和白色
        var type = 'line';
        if(currentYear == parseInt(y)) {
            randomColor = CHART_COLORS['red'];
            type = 'bar';
        }

        datasets.push({
            type: type,
            label: `(${y})${search_country}`,
            data: chartData.datasets.filter(sets => sets.label.includes(y))[0].data,
            backgroundColor: randomColor,
            borderColor: randomColor,
            borderWidth: 1
        });
    });

    return  {
        x_axis: chartData.x_axis,
        datasets: datasets
    };
}

function getChartData(dateType, startDate, endDate, search_country, search_product, yearSelectList) {
    var chartData = {
        x_axis: [],
        datasets: []
    };

    if(search_country == "" || search_product == "") {
        return chartData;
    }

    x_axis = getXaxis(dateType, startDate, endDate);
    //console.log(x_axis);

    var reports = {}
    const currentYear = new Date().getFullYear();

    var data = dateType == "week" ? DB_QuantumWeek : DB_Quantums;
    var searchAttr = dateType == "week" ? "yyyyww" : "Date";
    var cityAttr = dateType == "week" ? "country" : "Country";
    var itemAttr = dateType == "week" ? "itemcode" : "Itemcode";
    data.forEach(function(element) {
        var dt = element[searchAttr].split("-");
        var tYear = dt[0];
        if(element[cityAttr] === search_country && element[itemAttr] == search_product) {
            var resultKey = "result" + tYear;
            if (reports[resultKey]) {
                reports[resultKey].push(element);
            } else {
                reports[resultKey] = [element];
            }
        }
    });

    //console.log(reports);

    var xxValues = {};
    if(dateType == "week") {
        x_axis.forEach(function(element){
            var week = element.split("-")[1];
            for(var year in reports) {
                var yearStr = year.substring(6, 10);
                var resultKey = "result" + yearStr;
                if(xxValues[yearStr] == null) {
                    xxValues[yearStr] = [];
                }
                var tmp = reports[resultKey].find(item => item.yyyyww == yearStr + "-" + week.toString().padStart(2, "0"));
                if(tmp != null) {
                    xxValues[yearStr].push(tmp.total);
                } else {
                    xxValues[yearStr].push(0);
                }
            }
        });
    } else if(dateType == "day") {
        x_axis.forEach(function(element){
            for(var year in reports) {
                var yearStr = year.substring(6, 10);
                var resultKey = "result" + yearStr;
                if(xxValues[yearStr] == null) {
                    xxValues[yearStr] = [];
                }
                var tmp = reports[resultKey].find(item => moment(item.Date).format('MM-DD') == moment(element).format('MM-DD'));
                if(tmp != null) {
                    xxValues[yearStr].push(tmp.Weights);
                } else {
                    xxValues[yearStr].push(0);
                }
            }
        });
    }

    var datasets = [];
    yearSelectList.forEach(function(y){
        var randomColor = getRandomColor(['#000000', '#FFFFFF']); // 排除黑色和白色
        var type = 'line';
        if(currentYear == parseInt(y)) {
            randomColor = CHART_COLORS['red'];
            type = 'bar';
        }

        datasets.push({
            type: type,
            label: `(${y})${search_country}`,
            data: xxValues[y],
            backgroundColor: randomColor,
            borderColor: randomColor,
            borderWidth: 1
        });
    });

    chartData.x_axis = x_axis;
    chartData.datasets = datasets;
    //console.log(chartData)
    return chartData;
}

function GerDateWeek(currentDate) {
    var startDate = new Date(currentDate.getFullYear(), 0, 1);
    return Math.floor((currentDate - startDate) /
        (24 * 60 * 60 * 1000));
}

function getRandomColor(excludedColors) {
    var letters = "0123456789ABCDEF";
    var color;
    do {
      color = "#";
      for (var i = 0; i < 6; i++) {
        color += letters[Math.floor(Math.random() * 16)];
      }
    } while (excludedColors.indexOf(color) >= 0 || color === "#FF0000");
    return color;
  }

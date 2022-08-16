function getXaxis(startDate, enDate) {
    console.log(startDate.getDate(),  enDate.getDate())
    if(startDate.getMonth() != enDate.getMonth()) {
        return paddingDate(startDate).concat(paddingDate(enDate, "reverse"));
    } else if(startDate.getMonth() == enDate.getMonth() && startDate.getDate() < enDate.getDate()) {
        return paddingTwoDate(startDate, enDate);
    } else {
        return paddingDate(enDate, "reverse");
    }
}

function paddingDate(dt, direction) {
    var Xaxis = [];
    var month = dt.getMonth(); // 0~11
    var day = dt.getDate();
    var year = dt.getFullYear();
    var daysInMonth = new Date(year, month + 1, 0).getDate();

    //positive
    var startInx = day;
    if(direction == "reverse")
        startInx = 1;

    for(var i = startInx; i <= daysInMonth; i++)
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
    var datasets = [];
    var colorIdx = 0;
    var colorList = ['orange', 'green', 'blue', 'purple', 'grey', 'yellow'];
    yearSelectList.forEach(function(y){
        var label = "";
        var data = [];
        var randomColor = CHART_COLORS[colorList[colorIdx]];
        var type = 'line';
        if(parseInt(y) == 2022) {
            label = "(2022)"+search_country;
            data = chartData.datasets.filter(sets => sets.label.includes('2022'))[0].data;
            randomColor = CHART_COLORS['red'];
            type = "bar";
        } else if(parseInt(y) == 2021) {
            label = "(2021)"+search_country;
            data = chartData.datasets.filter(sets => sets.label.includes('2021'))[0].data;
        } else if(parseInt(y) == 2020) {
            label = "(2020)"+search_country;
            data = chartData.datasets.filter(sets => sets.label.includes('2020'))[0].data;
        } else if(parseInt(y) == 2019) {
            label = "(2019)"+search_country;
            data = chartData.datasets.filter(sets => sets.label.includes('2019'))[0].data;
        }
        datasets.push({
            type: type,
            label: label,
            data: data,
            backgroundColor: randomColor,
            borderColor: randomColor,
            borderWidth: 1
        });
        colorIdx++;
    });

    return  {
        x_axis: chartData.x_axis,
        datasets: datasets
    };
}

function getChartData(startDate, endDate, search_country, search_product, yearSelectList) {
    var chartData = {
        x_axis: [],
        datasets: []
    };

    if(search_country == "" || search_product == "") {
        return chartData;
    }

    x_axis = getXaxis(startDate, endDate);
    console.log(x_axis);

    var reports = {
        result19: [],
        result20: [],
        result21: [],
        result22: [],
    }

    DB_Quantums.forEach(function(element){
        var dt = element.date.split("-");
        var tYear = dt[0];
        if(element.country === search_country && element.itemcode == search_product) {
            if(tYear == 2019) {
                reports.result19.push(element);
            } else if(tYear == 2020) {
                reports.result20.push(element);
            } else if(tYear == 2021) {
                reports.result21.push(element);
            } else if(tYear == 2022) {
                reports.result22.push(element);
            }
        }
    });
    //console.log(reports);

    xxValues22 = [];
    xxValues21 = [];
    xxValues20 = [];
    xxValues19 = [];
    x_axis.forEach(function(element){
        var tmp = reports.result22.find(item => item.date == element);
        if(tmp != null) {
            xxValues22.push(tmp.weights);
        } else {
            xxValues22.push(0);
        }
        var tmp2 = reports.result21.find(item => moment(item.date).format('MM-DD') == moment(element).format('MM-DD'));
        if(tmp2 != null) {
            xxValues21.push(tmp2.weights);
        } else {
            xxValues21.push(0);
        }
        var tmp3 = reports.result20.find(item => moment(item.date).format('MM-DD') == moment(element).format('MM-DD'));
        if(tmp3 != null) {
            xxValues20.push(tmp3.weights);
        } else {
            xxValues20.push(0);
        }
        var tmp4 = reports.result19.find(item => moment(item.date).format('MM-DD') == moment(element).format('MM-DD'));
        if(tmp4 != null) {
            xxValues19.push(tmp4.weights);
        } else {
            xxValues19.push(0);
        }
    });

    var datasets = [];
    var colorIdx = 0;
    var colorList = ['orange', 'green', 'blue', 'purple', 'grey', 'yellow'];
    yearSelectList.forEach(function(y){
        var label = "";
        var data = [];
        var randomColor = CHART_COLORS[colorList[colorIdx]];
        var type = 'line';
        if(parseInt(y) == 2022) {
            label = "(2022)"+search_country;
            data = xxValues22;
            randomColor = CHART_COLORS['red'];
            type = 'bar';
        } else if(parseInt(y) == 2021) {
            label = "(2021)"+search_country;
            data = xxValues21;
        } else if(parseInt(y) == 2020) {
            label = "(2020)"+search_country;
            data = xxValues20;
        } else if(parseInt(y) == 2019) {
            label = "(2019)"+search_country;
            data = xxValues19;
        }
        datasets.push({
            type: type,
            label: label,
            data: data,
            backgroundColor: randomColor,
            borderColor: randomColor,
            borderWidth: 1
        });
        colorIdx++;
    });

    chartData.x_axis = x_axis;
    chartData.datasets = datasets;
    //console.log(chartData)
    return chartData;
}


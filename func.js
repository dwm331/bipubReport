function getXaxis(startDate, enDate) {
    return paddingDate(startDate).concat(paddingDate(enDate, "reverse"));
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

function numberPadZero(num) {
    return num.toString().padStart(2,"0");
}
Пример вызова утилиты из командной строки:
	PolygonUnwrapper.exe -f input.obj -w 50 -h 800 -s 5 -l 1:10
где 
	«-f input.obj» — путь к исходной модели в формате obj, обязательный параметр;
	«-w 50» — ширина одной страницы плоской сетки полигонов, обязательный параметр;
	«-h 80» — ширина одной страницы плоской сетки полигонов, обязательный параметр;
	«-s 5» — отступ между полигонами, а также от границ страницы, опциональный параметр, по-умолчанию равен 3;
	«-l 1:100» — диапазон загружаемых полигонов, здесь с 1-го по 100-й. Если указывать вместо диапазона просто число,
	то оно будет считаться верхней границей. Необязательнвый параметр, без него будут выгружены все полигоны.

В результате в папке с утилитой создаются три файла:
	«model.obj» — исходная модель, в которой каждый полигон заключён в нумерованую группу.
	«grid.obj» — те же полигоны в тех же нумерованных группах, но расположеных плоско постранично на квадратной сетке.
	«polygons+names.dxf» — полигоны с подписями, разложенные по разным слоям в векторном формате.
	«info.txt» — информация о периметре и площади отдельных полигонов и суммарные значения для всей модели.

В случае, если полигоны состоят более чем из трёх углов, они разбиваются на отдельные треугольники.
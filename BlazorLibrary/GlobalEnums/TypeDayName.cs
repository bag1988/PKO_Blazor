namespace BlazorLibrary.GlobalEnums
{
    public enum TypeDayName
    {
        ALLWAYS = 0,    //тип дня = всегда
        WORKDAY = 1,    //тип дня = рабочие дни
        WEEKEND = 2,    //тип дня = выходные дни
        HOLYDAY = 3,    //тип дня = праздники
        SELECTED = 4   //тип дня = выборочно
    }

    public enum Days
    {
        Su = 0, //ВС
        Mo = 1, //ПН
        Tu = 2, //ВТ
        We = 3, //СР
        Th = 4, //ЧТ
        Fr = 5,    //ПТ
        Sa = 6    //СБ
    }
}

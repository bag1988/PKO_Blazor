namespace BlazorLibrary.GlobalEnums
{
    public class DevType
    {
        //Типы устройств
        // 0xNN10 - зарезервировано под устройство УУЗС, где NN определяет его(УУЗС) подтип 
        // 0xFNNN - зарезервировано под устройства OmegaK(система записи)
        public const int NONE = 0x0000;
        public const int ASO = 0x0001;
        public const int P16x = 0x0008;
        public const int SZS = 0x0010;
        public const int CrossSZS8 = 0x0011;
        public const int RDM = 0x0020;
        public const int UZS = 0x8010;
        public const int RAD_MOD = 0xA010;
        public const int XPORT = 0xB010;       // УЗС   через Ethernet
        public const int XPORTUDP = 0xB110;    // УЗС   через Ethernet UDP
        public const int XPORTTCP = 0xB210;    // УЗС   через Ethernet TCP
        public const int UXPORT = 0xC010;      // УУЗС  через Ethernet
        public const int CrossUXPORT = 0xC011; // кросс через Ethernet для UXPORT,UXPORT_SRS
        public const int UXPORT_SRS = 0xC012;  // СРС   через Ethernet
        public const int PDUUXPORT = 0xC015;
        public const int UXPORT_P160T = 0xC016;
        public const int UXPORT_P164T = 0xC017;
        public const int UXPORT_P166T = 0xC018;
        public const int UXPORT_P160R = 0xC019;
        public const int UXPORT_P16xR = 0xC01A;
        public const int UXPORT_UXPORT = 0xC01B;
        public const int UXPORT_P164R = 0xC020;
        public const int UXPORT_P166R = 0xC021;
        public const int CrossP16 = 0xC022;    // кросс через Ethernet для PDUUXPORT
        //                         ,UXPORT_P160T,UXPORT_P164T,UXPORT_P166T
        //                         ,UXPORT_P160R,UXPORT_P16xR,UXPORT_P164R,UXPORT_P166R
        //                         ,UXPORT_UXPORT
        public const int EmulSZS = 0x3810;     // Эмуляция УУЗС
    }
}

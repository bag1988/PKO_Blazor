
//export function CreateIntersectionObserver(elem, ref) {
//    let options = {
//        root: elem,
//        threshold: 0
//    }

//    let callback = function (entries, observer) {
//        entries.forEach(async entry => {
//            // если элемент является наблюдаемым
//            if (entry.isIntersecting) {
//                //const child = entry.target;                
//                //console.log("unobserve", entry.target);
//                ////observer.unobserve(child);
//                //observer.unobserve(entry.target);
//                //observer.disconnect();
//                await ref.invokeMethodAsync("LoadData");
//                //console.log("observe", entry.target);
//                //observer.observe(entry.target);
//            }
//        });
//    }

//    let observer = new IntersectionObserver(callback, options);

//    if (elem.querySelector(".elem-add-data")) {
//        let target = elem.querySelector(".elem-add-data");
//        console.log("observe", target);
//        observer.observe(target);
//    }

//}

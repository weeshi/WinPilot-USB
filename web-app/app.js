(() => {
"use strict";
const catalog=window.WinPilotCatalog;
if(!catalog||!Array.isArray(catalog.apps))throw new Error("WinPilot catalog-data.js is missing or invalid");
const apps=catalog.apps;
document.querySelector(".system-strip span:nth-child(3)").textContent="V"+catalog.version;
const selected=new Set(), list=document.getElementById("appList"), search=document.getElementById("searchInput"), clearSearch=document.getElementById("clearSearch"), selectAll=document.getElementById("selectAll"), install=document.getElementById("installButton"), count=document.getElementById("selectionCount"), resultCount=document.getElementById("resultCount"), empty=document.getElementById("emptyState"), status=document.getElementById("statusText"), backdrop=document.getElementById("dialogBackdrop"), dialogTitle=document.getElementById("dialogTitle"), dialogDescription=document.getElementById("dialogDescription"), dialogSelection=document.getElementById("dialogSelection"), toast=document.getElementById("toast");
let lastFocus=null, toastTimer;
const normalize=s=>s.toLocaleLowerCase("ar").normalize("NFKD");
const visibleApps=()=>apps.filter(a=>normalize(a.name).includes(normalize(search.value.trim())));
function saveQuery(value){try{const url=new URL(location.href);value?url.searchParams.set("q",value):url.searchParams.delete("q");history.replaceState(null,"",url)}catch{}}
function announce(message){toast.textContent=message;toast.classList.add("show");clearTimeout(toastTimer);toastTimer=setTimeout(()=>toast.classList.remove("show"),1800)}
function updateSelection(){
 const visible=visibleApps(), selectedVisible=visible.filter(a=>selected.has(a.id)).length;
 count.textContent=selected.size+" محدد"; install.disabled=selected.size===0;
 selectAll.checked=visible.length>0&&selectedVisible===visible.length; selectAll.indeterminate=selectedVisible>0&&selectedVisible<visible.length;
 status.textContent=selected.size?"تم تحديد "+selected.size+" برنامج — جاهز للمتابعة":"جاهز — اختر البرامج المطلوبة";
}
function render(){
 const visible=visibleApps(); list.replaceChildren();
 visible.forEach(app=>{const label=document.createElement("label");label.className="app-row"+(app.end?" category-end":"");const checkbox=document.createElement("input");checkbox.type="checkbox";checkbox.checked=selected.has(app.id);checkbox.dataset.id=app.id;const name=document.createElement("span");name.textContent=app.name;const type=document.createElement("small");type.textContent=app.type;label.append(checkbox,name,type);list.append(label)});
 resultCount.textContent=visible.length+" / "+apps.length;empty.hidden=visible.length!==0;list.hidden=visible.length===0;search.parentElement.classList.toggle("has-value",search.value.length>0);updateSelection();
}
list.addEventListener("change",e=>{if(!e.target.matches("input"))return;e.target.checked?selected.add(e.target.dataset.id):selected.delete(e.target.dataset.id);updateSelection()});
search.addEventListener("input",()=>{render();saveQuery(search.value)});
clearSearch.addEventListener("click",()=>{search.value="";render();search.focus();saveQuery("")});
selectAll.addEventListener("change",()=>{visibleApps().forEach(a=>selectAll.checked?selected.add(a.id):selected.delete(a.id));render()});
document.getElementById("clearSelection").addEventListener("click",()=>{selected.clear();render();announce("تم إلغاء التحديد")});
function openDialog(title,description,names=[]){lastFocus=document.activeElement;dialogTitle.textContent=title;dialogDescription.textContent=description;dialogSelection.replaceChildren(...names.map(n=>{const x=document.createElement("span");x.textContent=n;return x}));backdrop.hidden=false;document.body.style.overflow="hidden";document.getElementById("dialogClose").focus()}
function closeDialog(){backdrop.hidden=true;document.body.style.overflow="";lastFocus?.focus()}
install.addEventListener("click",()=>openDialog("التثبيت يحتاج تطبيق Windows","المتصفح لا يستطيع تشغيل ملفات EXE أو طلب صلاحيات Administrator مباشرة. حُفظ اختيارك أدناه؛ نزّل تطبيق Windows ثم اختر البرامج نفسها لبدء التثبيت الصامت.",apps.filter(a=>selected.has(a.id)).map(a=>a.name)));
document.querySelectorAll("[data-tool]").forEach(button=>button.addEventListener("click",()=>openDialog(button.dataset.tool,"هذه الأداة تغيّر إعدادات Windows أو تشغّل ملفًا محليًا بصلاحيات Administrator، ولذلك تتوفر بأمان داخل تطبيق Windows فقط.")));
document.getElementById("dialogClose").addEventListener("click",closeDialog);document.getElementById("dialogCancel").addEventListener("click",closeDialog);backdrop.addEventListener("click",e=>{if(e.target===backdrop)closeDialog()});document.addEventListener("keydown",e=>{if(backdrop.hidden)return;if(e.key==="Escape"){closeDialog();return}if(e.key==="Tab"){const focusable=[...backdrop.querySelectorAll("button,a[href]")].filter(x=>!x.disabled);const first=focusable[0],last=focusable[focusable.length-1];if(e.shiftKey&&document.activeElement===first){e.preventDefault();last.focus()}else if(!e.shiftKey&&document.activeElement===last){e.preventDefault();first.focus()}}});
document.querySelector("[data-contact=facebook]").addEventListener("click",()=>announce("لم يتم تحديد رابط Facebook بعد"));
document.querySelectorAll('a[target="_blank"]').forEach(a=>a.rel="noopener noreferrer");
const initial=new URLSearchParams(location.search).get("q")||"";search.value=initial;render();
const ad=document.querySelector(".ad-frame img"), ads=["../Ads/dsa1.xxx","../Ads/dsa2.xxx","../Ads/dsa3.xxx"];let adIndex=0;setInterval(()=>{adIndex=(adIndex+1)%ads.length;ad.src=ads[adIndex]},7000);
})();

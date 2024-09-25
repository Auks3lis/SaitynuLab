# SaitynuLab
# 1.	Projekto aprašymas
Kuriamas projektas – minimali patiekalų receptų sistema (Dish -> recipie -> comment). 
## 1.1.	Sistemos paskirtis
Sistema yra skirta vartotojams kurti bei atrasti juos dominančius patiekalų receptus:
	-	Vartotojas pagal tam tikras patiekalų kategorijas gali atrasti receptus, po kuriais galima palikti komentarus/atsiliepimus.
  -	Administratorius gali kurti ne tik patiekalų kategorijas, bet ir į jas pridėti atitinkamus receptus.
  -	Svečias gali tik stebėti esamus receptus.
## 1.2.	Funkciniai reikalavimai
Vienas iš funkcinių reikalavimų – galimybė valdyti visus tris resursus (dish, recipie, comment) atitinkamai pagal savo roles. Taip pat reikia realizuoti vartotojo autentifikaciją ir autorizaciją. 

# 2.	Technologijų aprašymas
Projekto ‚Frontend‘ bus kuriamas su React technologija. Duomenų bazė – PostgreSQL. Serverio pusė - naudojant .Net Core Web API

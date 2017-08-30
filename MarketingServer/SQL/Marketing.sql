CREATE TABLE Niches(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(20) NOT NULL,
	LeadPage varchar(255)
);

CREATE TABLE Campaigns(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Name varchar(20) NOT NULL,
	NicheID int NOT NULL,
	FOREIGN KEY (NicheID) REFERENCES Niches(ID)
);

CREATE TABLE Customers(
	Email varchar(50) NOT NULL,
	NicheID int NOT NULL,
	Name varchar(50) NOT NULL,
	CampaignID int NOT NULL,
	CurrentCampaignDay int NOT NULL,
	EmailSendDate datetime NOT NULL,
	PRIMARY KEY(Email, NicheID),
	FOREIGN KEY (NicheID) REFERENCES Niches(ID),
	FOREIGN KEY (CampaignID) REFERENCES Campaigns(ID)
);



CREATE TABLE Emails(
	ID int NOT NULL PRIMARY KEY IDENTITY(1,1),
	Subject varchar(50) NOT NULL,
	Body varchar(MAX) NOT NULL,
	CampaignID int NOT NULL,
	Day int NOT NULL,
	FOREIGN KEY (CampaignID) REFERENCES Campaigns(ID)
);

Create NonClustered Index Customers_EmailSendDate on Customers(EmailSendDate)

Create NonClustered Index Emails_CampaignID_Day on Emails(CampaignID, Day)




SELECT Name, Email, CampaignID, CurrentCampaignDay
FROM Customers
WHERE EmailSendDate = DATEADD(day, DATEDIFF(day, 0, GETDATE()), 0)

SELECT Subject, Body
FROM Emails
WHERE CampaignID = 13 AND Day = 2


drop table Customers
drop table Emails
drop table Campaigns
drop table niches

select * from customers
select * from emails








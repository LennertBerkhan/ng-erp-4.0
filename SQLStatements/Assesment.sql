use TestResultContext;
go
-- Delete old stuff
DROP TABLE IF EXISTS dbo.#Timeliness
DROP TABLE IF EXISTS dbo.#StockValue
DROP TABLE IF EXISTS dbo.#ThroughputTime
DROP TABLE IF EXISTS dbo.#Utilization
DROP TABLE IF EXISTS dbo.#Setup
DROP TABLE IF EXISTS dbo.#Orders
go

-- Timeliness
select * into #Timeliness
from (select SimulationNumber, Value as 'Timeliness' from Kpis where KpiType = 3 and Name = 'timeliness') as x
-- Bound Capital
select * into #StockValue
from (select Sum(Value) as 'StockValue', SimulationNumber from Kpis where KpiType = 10 and Name in ('Assembly', 'Product') group by SimulationNumber) as x
 --ThroughputTime
 select * into #ThroughputTime
from (select SimulationNumber, Value as 'ThroughPutTime' from Kpis where KpiType = 0) as x 
-- Utilization
select * into #Utilization
	from (select  SimulationNumber,AVG(Value) as 'UtilizationAverage' from kpis
	  	  where (KpiType = 8)
		  group by SimulationNumber) as x
-- Setup
select * into #Setup
	from (select  SimulationNumber,AVG(Value) as 'SetupAverage' from kpis
	  	  where (KpiType = 9)
		  group by SimulationNumber) as x
-- Bound Capital
select * into #Orders
from (select Value as 'OrdersProcessed', ValueMax as 'OrdersOpen', SimulationNumber from Kpis where KpiType = 3 and Name = 'OrderProcessed') as x

-- Merge
Select t.SimulationNumber, Timeliness, StockValue, ThroughPutTime, UtilizationAverage, SetupAverage, OrdersProcessed, OrdersOpen
from #Timeliness t
join #StockValue sv on sv.SimulationNumber = t.SimulationNumber
join #ThroughputTime tt on tt.SimulationNumber = t.SimulationNumber
join #Utilization u on u.SimulationNumber = t.SimulationNumber
join #Setup s on s.SimulationNumber = t.SimulationNumber
join #Orders o on o.SimulationNumber = t.SimulationNumber
order by SimulationNumber




-- debug tracing.
select distinct count(CreatedForOrderId) from SimulationJobs
group by CreatedForOrderId

select CreatedForOrderId, Count(CreatedForOrderId) from SimulationJobs
group by CreatedForOrderId
having count(CreatedForOrderId) > 25

select * from SimulationJobs where CreatedForOrderId in ('[17812]')
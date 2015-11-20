

(**

# MBrace.Azure and HDInsight

One nice thing about MBrace.Azure is that you can use the same Azure storage account as with any HDInsight cluster and have access to the same files for large scale processing. As an example the following Spark code performs word count on the HVAC.csv sample dataset.



*)

val hvacText = sc.textFile("wasb://sparkdemonpal@npalmbracetest.blob.core.windows.net/HdiSamples/SensorSampleData/hvac/HVACBIG.csv")


val result = hvacText.map(s => s.split(","))
		     .filter(s => s(0) != "Date")
             .map(s => (s(0), 1))
             .countByKey;


(**

In MBrace.Azure we can use the same storage account and accesss the same datasource with pretty much the same code.

*)

let result = 
    CloudFlow.OfCloudFileByLine("HdiSamples/SensorSampleData/hvac/HVACBIG.csv")
    |> CloudFlow.map (fun s -> s.Split(','))
    |> CloudFlow.filter (fun s -> s.[0] <> "Date")
    |> CloudFlow.countBy (fun s -> s.[0])
    |> Cluster.Run


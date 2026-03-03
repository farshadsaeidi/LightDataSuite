const loadGraphData = async (id: number) => {
    try {
        const request: DataRequest = {
            query: `{ 
                      masteraccounts(customerid: $id) { 
                        firstname lastname 
                        invoices(on: "customerid", parentkey: "customerid", first: 2) { 
                          invoiceid amount 
                            } 
                        } 
                    }`,
            id: id // Replaces $id in query
        };
        const result = await fetchData<Account>("graphql", request);
        setAccounts(result);
    } catch (err: any) {
        setError(err.message);
    }
};

const loadKqlData = async (id: number) => {
    try {
        const request: DataRequest = {
            // The KQL version of your nested GraphQL query
            query: `masteraccounts 
                    | where customerid == $id 
                    | project firstname, lastname, customerid, 
                      invoices = (invoices | where customerid == $id | take 2 | project invoiceid, amount, createdate)`,
            id: id // Replaces $id in the query string
        };

        const result = await fetchData<Account>("kql", request);
        setAccounts(result);
    } catch (err: any) {
        setError(err.message);
    }
};

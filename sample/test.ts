export interface DataRequest {
    query: string;
    id?: number;
}

export interface Account {
    firstname?: string;
    lastname?: string;
    customerid?: number;
    invoices?: Array<{
        invoiceid: number;
        amount: number;
        createdate?: string;
    }>;
}

const API_BASE = "https://localhost:7190";

const fetchData = async <T>(endpoint: string, request: DataRequest): Promise<T> => {
    const response = await fetch(`${API_BASE}${endpoint}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(request)
    });

    if (!response.ok) {
        throw new Error(`API Error: ${response.status}`);
    }

    return response.json() as Promise<T>;
};

export const loadGraphData = async (id: number): Promise<Account[]> => {
    try {
        const request: DataRequest = {
            query: `{ 
                      masteraccounts(customerid: $id) { 
                        firstname lastname 
                        invoices(customerid: $id, first: 2) {
                          invoiceid amount createdate
                            } 
                        } 
                    }`,
            id: id
        };
        const result = await fetchData<Account[]>("/GQL", request);
        return result;
    } catch (err: any) {
        throw new Error(err.message);
    }
};

export const loadKqlData = async (id: number): Promise<Account[]> => {
    try {
        const request: DataRequest = {
            query: `masteraccounts 
                    | where customerid == $id 
                    | project firstname, lastname,  
                      invoices = (invoices
                          | where customerid == $id
                          | take 2
                          | project invoiceid, amount, createdate
                      )`,
            id: id
        };
        const result = await fetchData<Account[]>("/KQL", request);
        return result;
    } catch (err: any) {
        throw new Error(err.message);
    }
};


/*
        const request: DataRequest = {
            query: `{ 
                      masteraccounts(customerid: $id) { 
                        firstname lastname 
                        invoices(on: "customerid", parentkey: "customerid", first: 2) { 
                          invoiceid amount 
                            } 
                        } 
                    }`,
            id: id
        };
*/
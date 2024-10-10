import { useState, useEffect } from 'react';
import { Paper, Button } from '@mui/material';
import { DataGrid } from '@mui/x-data-grid';
import moment from 'moment';
import './App.css'

function App() {

  const [statusData, setStatusData] = useState();

  const fetchData = () => {
    fetch('/status').then((result) => {
      return result.json();
    }).then((json) => {
      setStatusData(json);
    }).catch((e) => {
      console.log(e);
    });
  }

  // Fetch status JSON on first render
  useEffect(() => {
    fetchData();
  }, []);

  // Instead of showing dates and times show "N minutes ago" or similar
  const relativeDate = (date) => {
    // If no queued messages date comes back as 0001-01-01
    if (!moment(date).isSame('0001-01-01', 'day')) {
      // Time comes from server in UTC but without the Z indicating such, we append it
      return moment(`${date}Z`).fromNow();
    }
  }

  const columns = [
    { field: 'jurisdictionId', headerName: 'Jurisdiction', width: 121 },
    { field: 'messageCount', headerName: '# Messages', type: 'number', valueGetter: (value, row) => row.processedCount + row.queuedCount, width: 130 },
    { field: 'receivedFiveMinutes', headerName: 'Received last 5min', type: 'number', valueGetter: (value, row) => row.processedCountFiveMinutes + row.queuedCountFiveMinutes, width: 140 },
    { field: 'receivedOneHour', headerName: 'Received last 1hr', type: 'number', valueGetter: (value, row) => row.processedCountOneHour + row.queuedCountOneHour, width: 140 },
    { field: 'processedCount', headerName: '# Processed', type: 'number', width: 130 },
    { field: 'processedCountFiveMinutes', headerName: 'Processed last 5min', type: 'number', width: 150 },
    { field: 'processedCountOneHour', headerName: 'Processed last 1hr', type: 'number', width: 140 },
    { field: 'queuedCount', headerName: '# Queued', type: 'number', width: 110 },
    { field: 'oldest', headerName: 'Oldest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.oldestQueued), width: 140 },
    { field: 'newest', headerName: 'Newest Queued', sortable: false, valueGetter: (value, row) => relativeDate(row.newestQueued), width: 140 },
  ];

  const allJurisdictionRows = statusData ? [{ jurisdictionId: 'All', ...statusData }] : [];
  const jurisdictionRows = statusData?.jurisdictionResults || [];

  return (
    <>
      <Button variant="contained" sx={{ mb: 5 }} onClick={() => fetchData()}>Refresh</Button>
      
      <Paper sx={{ width: '100%', mb: 5 }}>
        <DataGrid
          rows={allJurisdictionRows}
          getRowId={(row) => row.jurisdictionId }
          columns={columns}
          initialState={{
            sorting: {
              sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
            },
          }}
        />
      </Paper>

      <Paper sx={{ width: '100%' }}>
        <DataGrid
          rows={jurisdictionRows}
          getRowId={(row) => row.jurisdictionId }
          columns={columns}
          initialState={{
            sorting: {
              sortModel: [{ field: 'jurisdictionId', sort: 'asc' }],
            },
          }}
        />
      </Paper>
    </>
  )
}

export default App
